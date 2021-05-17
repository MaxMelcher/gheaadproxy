# GitHub Enterprise Server with Azure Active Directory Authentication (GHEAADPROXY)

With GitHub Enterprise Server Version 3.0.6 as self-hosted VM, you can configure SAML authentication backed by Azure Active Directory. 
This protects the web interface, you get conditional access policies with Multi Factor Authentiction powered by Azure Active Directory. 
Sessions do expire after a configurable lifetime, then the user must re-authenticate.

Unfortunately, this does not apply for the git side of things. If you clone a repository, pull or push a username and PAT token or ssh key is being accepted. 
There is no session or multi-factor enforcement. 

With this **proof of concept (PoC)** I want to showcase that it is possible to put a proxy in front of GitHub Enterprise Server that enforces MFA (based on the configuration in Azure Active Directory) and that the session expires.
