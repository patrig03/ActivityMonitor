# Activity Monitor - Firefox Extension

This extension tracks the active tab URL in Firefox and sends it to the Activity Monitor backend.

## Installation

1. Open Firefox and navigate to `about:debugging`
2. Click "This Firefox" in the left sidebar
3. Click "Load Temporary Add-on"
4. Navigate to this directory and select the `manifest.json` file

## Configuration

The extension sends data to: `http://127.0.0.1:8090/tab`

Make sure the Activity Monitor backend is running and listening on port 8090.

## Features

- Tracks active tab changes
- Monitors URL updates
- Detects window focus changes
- Prevents duplicate submissions of the same URL
