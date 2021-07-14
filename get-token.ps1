az login --allow-no-subscriptions --use-device-code
$bearer = az account get-access-token --resource api://253600a6-46b5-425f-ab7e-d70df34c97ae --query 'accessToken' -o tsv
git config http.extraHeader "MFA: bearer $($bearer)"
