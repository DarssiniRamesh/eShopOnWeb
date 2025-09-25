# eShopOnWeb Container

This container resolves the MSBUILD error:
`MSBUILD : error MSB1011: Specify which project or solution file to use because this folder contains more than one project or solution file.`

Fix applied:
- The Dockerfile explicitly restores, builds, and publishes using `eShopOnWeb.sln`.
- The runtime runs the Web entrypoint (`Web.dll`) to start the storefront.

How to build and run:

```bash
# From the repository root
docker build -f eShopOnWeb/Dockerfile -t eshoponweb:latest eShopOnWeb

docker run --rm -p 8080:8080 eshoponweb:latest
```

After the container starts, browse:
- http://localhost:8080
