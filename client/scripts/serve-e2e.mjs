process.env.HOST = '127.0.0.1';
process.env.PORT = '4173';
process.env.ORIGIN = 'http://127.0.0.1:4173';

await import('../build/index.js');
