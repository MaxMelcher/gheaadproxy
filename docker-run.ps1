dotnet publish -c Release
docker build -t acrmamelch.azurecr.io/gheaadproxy -f DOCKERFILE .
docker push acrmamelch.azurecr.io/gheaadproxy

docker run --rm -p 8000:80 -p 8001:443 acrmamelch.azurecr.io/gheaadproxy

docker login

helm uninstall gheaadproxy
helm upgrade --install gheaadproxy .\charts\ghe-aad-proxy\