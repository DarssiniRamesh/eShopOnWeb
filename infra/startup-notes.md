# Startup Notes

- The Dockerfile now restores/builds using `eShopOnWeb.sln` to prevent MSBuild ambiguity errors when multiple solutions or projects exist in the folder.
- `docker-compose.yml` is configured to use the Dockerfile explicitly and expose port `8080` internally (mapped to host ports 5106/5107 via compose).
- For a quick start without compose:

```bash
docker build -f eShopOnWeb/Dockerfile -t eshoponweb:latest eShopOnWeb
docker run --rm -p 8080:8080 eshoponweb:latest
```
