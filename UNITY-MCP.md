# Unity MCP

Configured package: CoplayDev MCP for Unity v10.3.0 in Packages/manifest.json.
Codex server name: unityMCP.
HTTP endpoint: http://localhost:8080/mcp.

## Start

The Editor bootstrap pins `UV_PYTHON=3.12` for child processes to avoid
Python 3.15 source-build failures in PyYAML. A manual fallback is
`powershell -File Tools/UnityMCP/Start-Server.ps1` from the project root.
The local uv/uvx executables are in `Tools/UnityMCP` (excluded from Git).

1. Restart Unity Hub so it picks up the updated user PATH, then open this project.
2. Wait for Package Manager to download the package and for compilation to finish.
3. Open Window > MCP for Unity and complete its setup wizard.
4. Select the local HTTP transport and confirm the URL is http://localhost:8080.
5. Start the server and connect the Unity bridge using the controls in that window.
6. Restart Codex or open a fresh chat in this project to load the new MCP tools.
7. Ask Codex to list the active scene and read Console errors to verify the connection.

If uv is not detected, use its executable path in the Unity MCP settings:
`C:/Users/Quytsus/AppData/Roaming/Python/Python312/Scripts/uv.exe`.
The matching uvx.exe is in the same directory.

MCP lets Codex access Unity Editor tools; chat remains in Codex.
Keep this server local. No OpenAI API key is needed for the Unity bridge itself.

## References

- https://coplaydev.github.io/unity-mcp/getting-started/install
- https://learn.chatgpt.com/docs/extend/mcp?surface=cli
