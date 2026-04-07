# Docker-Aufteilung (6 Container)

## Zielbild

1. `mcp` – MCP-Service (Port `8081`)
2. `wiki` – Wiki.js (Port `3000`)
3. `datacentermods-modstore` – Modstore-Frontend (Port `8090`)
4. `modstore-api` – fehlender Kernservice/API (intern, via Frontend Proxy)
5. `minio` – S3-kompatibler Objektspeicher (Ports `9000`/`9001`)
6. `postgres` – Datenbank für Wiki + Modstore (Port `5432`)

## Start

```powershell
Copy-Item .env.docker.example .env
docker compose up -d --build
```

## Stop

```powershell
docker compose down
```

## Persistente Daten

- `pg_data`
- `minio_data`
- `wiki_data`

## Health-Endpunkte

- MCP: `http://localhost:8081/health`
- Modstore API: `http://localhost:8090/api/health`
- Modstore Frontend: `http://localhost:8090`
- Wiki: `http://localhost:3000`
- MinIO Console: `http://localhost:9001`

## Hinweise

- `modstore-api` ist als Skeleton angelegt und bereit für DB/S3-Integration.
- Self-hosted S3 läuft über MinIO; später kann auf externes S3 umgestellt werden.
