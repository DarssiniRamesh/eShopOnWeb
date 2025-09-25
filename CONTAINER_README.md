# eShopOnWeb Container

This container resolves the MSBUILD error:
`MSBUILD : error MSB1011: Specify which project or solution file to use because this folder contains more than one project or solution file.`

Fix applied:
- The Dockerfile explicitly restores, builds, and publishes using `eShopOnWeb.sln`.
- The runtime runs the Web entrypoint (`Web.dll`) to start the storefront.

How to build and run:

Option A: Build and run the Web app image directly

```bash
# From the repository root
docker build -f eShopOnWeb/Dockerfile -t eshoponweb:latest eShopOnWeb
docker run --rm -p 8080:8080 eshoponweb:latest
```

Option B: Use docker-compose to build and run both Web and PublicApi

```bash
# From the repository root (where eShopOnWeb.sln exists)
docker compose build
docker compose up
# Web -> http://localhost:5106
# PublicApi -> http://localhost:5200
```

Notes:
- Both Dockerfiles (Web and PublicApi) explicitly restore/build against eShopOnWeb.sln and publish a specific project (Web/PublicApi) to avoid MSBUILD ambiguity.
- If you encounter login or HTTPS errors locally, try an incognito browser session or ensure dev certificates are installed.

After the container starts, browse:
- Web: http://localhost:5106 (compose) or http://localhost:8080 (single image)
- PublicApi: http://localhost:5200 (compose)
