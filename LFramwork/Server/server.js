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
      running = true;
      ws.send(JSON.stringify({ type: 'info', message: 'simulation started' }));
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
  console.log('[RVO-Server] client connected');

  ws.on('message', (data) => {
    const text = data.toString();

    // 先尝试按 JSON 解析，用于处理 Unity 发送的 spawn 消息
    try {
      const obj = JSON.parse(text);
      if (obj && obj.type === 'spawn') {
        // 直接广播给所有客户端，由各客户端统一生成 Agent
        broadcast(obj);
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

  ws.send(JSON.stringify({ type: 'hello', message: 'connected to RVO control server' }));
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
