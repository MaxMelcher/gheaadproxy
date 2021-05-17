dotnet publish -c Release
docker build -t acrmamelch.azurecr.io/gheaadproxy -f DOCKERFILE .
docker push acrmamelch.azurecr.io/gheaadproxy

docker run --rm -it -p 80:5000 -p 443:5001 --env-file=docker.env acrmamelch.azurecr.io/gheaadproxy

docker login

helm uninstall gheaadproxy
helm upgrade --install gheaadproxy .\charts\ghe-aad-proxy\