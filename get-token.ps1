az login --allow-no-subscriptions --use-device-code
az account get-access-token --resource api://253600a6-46b5-425f-ab7e-d70df34c97ae
git config http.extraHeader "MFA: bearer $($bearer)"
