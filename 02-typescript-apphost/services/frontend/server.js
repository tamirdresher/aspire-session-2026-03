/**
 * Simple Node.js/Express Frontend Service
 * Orchestrated by TypeScript AppHost
 */

const express = require('express');
const app = express();

// Service discovery: Aspire injects API URL as environment variable
const API_URL = process.env.services__api__http__0 || 'http://localhost:5000';
const PORT = process.env.PORT || 3000;

app.get('/', (req, res) => {
    res.send(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>TypeScript AppHost Demo</title>
            <style>
                body { font-family: Arial; max-width: 800px; margin: 50px auto; }
                h1 { color: #512BD4; }
                .info { background: #f0f0f0; padding: 15px; border-radius: 5px; }
            </style>
        </head>
        <body>
            <h1>🎉 TypeScript AppHost Example</h1>
            <div class="info">
                <p><strong>This is a Node.js service orchestrated by Aspire!</strong></p>
                <p>API Service: ${API_URL}</p>
                <p>Frontend Port: ${PORT}</p>
            </div>
            <p><a href="/api">Call .NET API</a></p>
            <p><a href="/health">Health Check</a></p>
        </body>
        </html>
    `);
});

app.get('/health', (req, res) => {
    res.json({ status: 'healthy', service: 'frontend', runtime: 'Node.js' });
});

app.get('/api', async (req, res) => {
    try {
        const fetch = (await import('node-fetch')).default;
        const response = await fetch(`${API_URL}/weatherforecast`);
        const data = await response.json();
        res.json({ source: 'api', data });
    } catch (error) {
        res.status(500).json({ error: error.message });
    }
});

app.listen(PORT, () => {
    console.log(`✅ Frontend running on http://localhost:${PORT}`);
    console.log(`📡 API endpoint: ${API_URL}`);
});
