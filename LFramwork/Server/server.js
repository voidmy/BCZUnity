// Simple WebSocket server for controlling RVO simulation from Unity
// Run with: npm install && npm start
//
// Text commands (send as plain text over WebSocket, or from a simple client):
//   start          - start simulation and begin broadcasting tick messages
//   stop           - stop simulation and stop broadcasting ticks
//   pause          - same as stop, pause broadcasting ticks
//   resume         - same as start, resume broadcasting ticks
//   setTick <int>  - set tick rate (ticks per second), e.g. "setTick 30"
//   status         - query current running state, tickRate, tickCount

const WebSocket = require('ws');
const os = require('os');

const PORT = process.env.PORT || 8081;

// Simulation state (server-side authoritative tick rate etc.)
let tickRate = 20; // ticks per second
let running = false;
let lastTickTime = Date.now();
let tickCount = 0;

// Simple incremental user id assignment, used by clients to know
// which connection was the first one.
let nextUserId = 1;

// Track per-tick state hashes reported by clients to detect desync.
// stateHashByTick[tick] = first hash value seen for that tick.
const stateHashByTick = new Map();

function printLocalIPs() {
  const nets = os.networkInterfaces();
  const results = [];

  for (const name of Object.keys(nets)) {
    for (const net of nets[name]) {
      if (net.family === 'IPv4' && !net.internal) {
        results.push(net.address);
      }
    }
  }

  if (results.length === 0) {
    console.log('[RVO-Server] No non-internal IPv4 address found');
  } else {
    console.log('[RVO-Server] Local IPv4 addresses:');
    for (const ip of results) {
      console.log(`  ws://${ip}:${PORT}`);
    }
  }
}

const wss = new WebSocket.Server({ port: PORT }, () => {
  console.log(`[RVO-Server] WebSocket server listening on ws://localhost:${PORT}`);
  printLocalIPs();
});

// Allow controlling the server directly from the terminal where `node server.js` is running.
// Type commands like: start, stop, pause, resume, setTick 30, status
process.stdin.setEncoding('utf8');
process.stdin.on('data', (chunk) => {
  const line = chunk.trim();
  if (!line) return;

  // Reuse the same command handler, but send feedback to console instead of a WebSocket client
  const consoleClient = {
    send: (msg) => {
      try {
        const obj = JSON.parse(msg);
        console.log('[RVO-Server][console]', obj);
      } catch (e) {
        console.log('[RVO-Server][console]', msg);
      }
    },
  };

  handleCommand(consoleClient, line);
});

function broadcast(obj) {
  const msg = JSON.stringify(obj);
  for (const client of wss.clients) {
    if (client.readyState === WebSocket.OPEN) {
      client.send(msg);
    }
  }
}

function handleCommand(ws, cmdLine) {
  const parts = cmdLine.trim().split(/\s+/);
  const cmd = parts[0]?.toLowerCase();

  switch (cmd) {
    case 'start':
      // Start a new simulation run from tick 0.
      // This avoids the situation where the server has accumulated a very large
      // tickCount before Unity connects, which would otherwise cause newly
      // scheduled spawn/skill messages (tick = tickCount + 1) to be applied
      // only after the client has replayed a long backlog of ticks.
      running = true;
      tickCount = 0;
      lastTickTime = Date.now();
      stateHashByTick.clear();
      ws.send(JSON.stringify({ type: 'info', message: 'simulation started (tickCount reset to 0)' }));

      // 通知所有客户端重置本地仿真状态，从完全一致的初始状态重新开始。
      broadcast({ type: 'reset' });
      break;
    case 'resume':
      running = true;
      ws.send(JSON.stringify({ type: 'info', message: 'simulation resumed' }));
      break;
    case 'stop':
      running = false;
      ws.send(JSON.stringify({ type: 'info', message: 'simulation stopped' }));
      break;
    case 'pause':
      running = false;
      ws.send(JSON.stringify({ type: 'info', message: 'simulation paused' }));
      break;
    case 'settick':
      if (parts.length >= 2) {
        const v = parseInt(parts[1], 10);
        if (!Number.isNaN(v) && v > 0 && v <= 200) {
          tickRate = v;
          ws.send(JSON.stringify({ type: 'info', message: `tickRate set to ${tickRate}` }));
        } else {
          ws.send(JSON.stringify({ type: 'error', message: 'invalid tick value' }));
        }
      } else {
        ws.send(JSON.stringify({ type: 'error', message: 'usage: setTick <int>' }));
      }
      break;
    case 'status':
      ws.send(JSON.stringify({
        type: 'status',
        running,
        tickRate,
        tickCount,
      }));
      break;
    default:
      ws.send(JSON.stringify({ type: 'error', message: `unknown command: ${cmdLine}` }));
      break;
  }
}

wss.on('connection', (ws) => {
  // Assign a unique userId to this connection.
  ws.userId = nextUserId++;
  console.log(`[RVO-Server] client connected, userId=${ws.userId}`);

  ws.on('message', (data) => {
    const text = data.toString();

    // 先尝试按 JSON 解析，用于处理 Unity 发送的 spawn 消息
    try {
      const obj = JSON.parse(text);
      if (obj && (obj.type === 'spawn' || obj.type === 'skill')) {
        // 为 spawn / skill 消息附加一个目标 tick，确保所有客户端在同一逻辑 tick 上执行
        // 使用当前 tickCount + 1，表示“从下一 tick 起生效”。
        const scheduled = {
          ...obj,
          tick: tickCount + 1,
        };

        // 直接广播给所有客户端，由各客户端在指定 tick 上统一生成 Agent 或释放技能
        broadcast(scheduled);
        return;
      }

      if (obj && obj.type === 'stateHash') {
        const tick = obj.tick;
        const hash = obj.hash;

        if (typeof tick === 'number' && typeof hash === 'number') {
          const existing = stateHashByTick.get(tick);
          if (existing === undefined) {
            stateHashByTick.set(tick, hash);

            // 为了避免内存无限增长，只保留最近的几千个 tick
            if (stateHashByTick.size > 5000) {
              const oldestKey = stateHashByTick.keys().next().value;
              stateHashByTick.delete(oldestKey);
            }
          } else if (existing !== hash) {
            console.warn(`[RVO-Server][DESYNC] tick ${tick} has mismatched hashes: first=${existing}, new=${hash}. Stopping simulation.`);

            // 一旦发现某个 tick 的 hash 不一致，立刻停止广播 tick
            running = false;

            // 广播一条 desync 消息给所有客户端，便于在 Unity 端提示
            broadcast({
              type: 'desync',
              tick,
              expectedHash: existing,
              newHash: hash,
            });
          }
        }

        return;
      }
    } catch (e) {
      // 不是 JSON，继续当作文本命令处理
    }

    // 默认按旧的文本命令处理，例如 "start", "setTick 30", "status"
    handleCommand(ws, text);
  });

  ws.on('close', () => {
    console.log('[RVO-Server] client disconnected');
  });

  ws.send(JSON.stringify({ type: 'hello', userId: ws.userId, message: 'connected to RVO control server' }));
});

// Simple tick loop, broadcasting tick events according to tickRate
setInterval(() => {
  if (!running) return;

  const now = Date.now();
  const dtMs = now - lastTickTime;
  const targetMs = 1000 / tickRate;

  if (dtMs >= targetMs) {
    const ticksToRun = Math.floor(dtMs / targetMs);
    lastTickTime = now;
    tickCount += ticksToRun;

    broadcast({
      type: 'tick',
      ticks: ticksToRun,
      tickRate,
      tickCount,
    });
  }
}, 5);

process.on('SIGINT', () => {
  console.log('\n[RVO-Server] shutting down');
  wss.close(() => process.exit(0));
});
