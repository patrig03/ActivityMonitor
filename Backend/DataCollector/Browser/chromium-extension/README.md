# Activity Monitor - Chromium Extension

This extension tracks the active tab URL in Chromium-based browsers (Chrome, Chromium, Edge, Brave, Opera, Vivaldi) and sends it to the Activity Monitor backend.

## Installation

1. Open your Chromium-based browser and navigate to:
   - Chrome: `chrome://extensions`
   - Edge: `edge://extensions`
   - Brave: `brave://extensions`
   - Opera: `opera://extensions`

2. Enable "Developer mode" (toggle in the top right)
3. Click "Load unpacked"
4. Navigate to this directory and select it

## Configuration

The extension sends data to: `http://127.0.0.1:8091/tab`

Make sure the Activity Monitor backend is running and listening on port 8091.

## Features

- Tracks active tab changes
- Monitors URL updates
- Detects window focus changes
- Prevents duplicate submissions of the same URL
- Uses Manifest V3 for compatibility with modern Chrome extensions
