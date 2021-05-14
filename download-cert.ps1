$webRequest = [Net.WebRequest]::Create("https://git.germanywestcentral.cloudapp.azure.com")
try { $webRequest.GetResponse() } catch {}
$cert = $webRequest.ServicePoint.Certificate
$bytes = $cert.Export([Security.Cryptography.X509Certificates.X509ContentType]::Cert)
set-content -value $bytes -encoding byte -path "$pwd\ghe.cer"