$ErrorActionPreference = 'Stop'
& "$PSScriptRoot\uvx.exe" --python 3.12 --from mcpforunityserver==10.3.0 mcp-for-unity --transport http --http-url http://127.0.0.1:8080 --project-scoped-tools
exit $LASTEXITCODE
