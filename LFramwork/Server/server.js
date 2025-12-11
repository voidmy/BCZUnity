// Simple WebSocket server for controlling RVO simulation from Unity
// Run with: npm install && npm start

const WebSocket = require('ws');

const PORT = process.env.PORT || 8080;

// Simulation state (server-side authoritative tick rate etc.)
let tickRate = 20; // ticks per second
let running = false;
let lastTickTime = Date.now();
let tickCount = 0;

const wss = new WebSocket.Server({ port: PORT }, () => {
  console.log(`[RVO-Server] WebSocket server listening on ws://localhost:${PORT}`);
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
    case 'stop':
      running = false;
      ws.send(JSON.stringify({ type: 'info', message: 'simulation stopped' }));
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
    let text = data.toString();
    // Expect simple text commands from Unity / tools, e.g. "start", "setTick 30", "status"
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
