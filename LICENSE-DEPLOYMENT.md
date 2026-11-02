# Offline HART POS license deployment

The POS app verifies a signed license locally. It does not contact a licensing server or require internet access.

## What users see

Until a matching license is installed, every MVC page redirects to `/License`. This public activation page shows the server's Installation ID and accepts a signed `.json` file. Once the signature, product, installation ID, and expiry are valid, the existing POS login and other pages are available. An expired, malformed, missing, or different-server license remains blocked.

The Installation ID is a SHA-256 fingerprint of the POS product and the Windows server's machine GUID. It is not a password or signing secret. Reinstalling Windows or moving the app to a different Windows server changes the ID and requires a newly issued license. Licenses are valid through their UTC expiry date; a blank expiry in the generator creates a perpetual license.

## Issuing and installing a license

1. Build and deploy the POS website on the customer's IIS server.
2. Visit `/License` and send the displayed Installation ID to the license issuer.
3. Use the separate offline `POSLicenseGenerator` on the issuer's computer to create a signed license for that ID.
4. On the IIS server, open an elevated PowerShell window and grant the app-pool identity access to the license directory:

   ```powershell
   & ".\HART POS\Scripts\Setup-LicenseFolder.ps1" -AppPoolName "HARTPOS"
   ```

   Run that command from the POS repository root; from the published website root, use `.\Scripts\Setup-LicenseFolder.ps1` instead. Replace `HARTPOS` with the actual IIS application pool name. The default license directory is `%ProgramData%\HARTPOS`. If the pool runs under a custom Windows account, pass it as `-Identity "DOMAIN\account"` too.
5. Upload the signed file on `/License`. The app verifies it before atomically installing it outside the website root.

If a different folder under `%ProgramData%` is needed, set the POS Web.config `LicenseDirectory` app setting to that folder and pass the same `-LicenseDirectory` value to the setup script. The application rejects a license folder within its website directory.

See the sibling [`POSLicenseGenerator` instructions](../POSLicenseGenerator/README.md) for key creation and license-generation commands.

## Signing key security

The verifier embeds only the 3072-bit public key. The corresponding private key must stay on the issuer's computer; never deploy, email, or commit it. The generator and POS verifier must use the same public/private key pair. Keep a protected backup of the private key: if it is lost, future licenses require a newly generated key pair and a POS deployment updated with the matching public key.

Licensing is an offline deterrent, not tamper-proof DRM. A person with administrator access to the IIS server can change deployed application files, the Windows clock, or the machine identity. Expiry enforcement depends on the server's UTC clock.
