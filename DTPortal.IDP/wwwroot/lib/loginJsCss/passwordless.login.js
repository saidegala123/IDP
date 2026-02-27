document.getElementById('FIDO2Form').addEventListener('submit', handleSignInSubmit);

async function handleSignInSubmit(Fido2Options) {
    event.preventDefault();
    $('#overlay2').show();
    if (!document.getElementById("FIDO2error").classList.contains("hide")) {
        document.getElementById("FIDO2error").classList.remove("show");
        document.getElementById("FIDO2error").classList.add("hide");
    }

    if (typeof Fido2Options != "string")
        Fido2Options = this.FIDOOption.value;

    let publicKeyOptions = JSON.parse(Fido2Options);

    const challenge = publicKeyOptions.challenge.replace(/-/g, "+").replace(/_/g, "/");
    publicKeyOptions.challenge = Uint8Array.from(atob(challenge), c => c.charCodeAt(0));

    publicKeyOptions.allowCredentials.forEach(function (listItem) {
        var fixedId = listItem.id.replace(/\_/g, "/").replace(/\-/g, "+");
        listItem.id = Uint8Array.from(atob(fixedId), c => c.charCodeAt(0));
    });

    /*$('#overlay').hide();*/
    // ask browser for credentials (browser will ask connected authenticators)
    let credential;
    try {
        credential = await navigator.credentials.get({ publicKey: publicKeyOptions });
        $('#overlay2').show();

        try {

            var response = await verifyAssertionWithServer(credential, publicKeyOptions);
            if (!response.success) {
                $('#overlay2').hide();
                showError(response.message)
                //swal({ type: 'info', title: response.message, text: "Click on 'Ok' button for login again!" }, function (isConfirm) {
                //    if (isConfirm) {
                //        deleteCookies();
                //        window.location.reload();
                //    }
                //});

            } else {
                $('#overlay2').hide();
                $('#overlay1').show();
                $('#registerBtn').attr("disabled", true);
                $('#registerBtn').css('opacity', '0.5');
                $('#cancle').attr("disabled", true);
                $('#cancle').css('opacity', '0.5');
                show();
            }

        } catch (e) {
            $('#overlay2').hide();
            showError("Something went wrong.! try again.")
        }
    } catch (err) {
        $('#overlay2').hide();
       /* showError("The operation either timed out or was cancled. We couldn’t verify you or the security key you use. please try again")*/
       
        swal({
            title: "Authentication Failed",
            text: "The operation either timed out or was cancled. We couldn’t verify you or the security key you use. please try again",
            button: "Close", // Text on button
            icon: "error", //built in icons: success, warning, error, info
            timer: 4000, //timeOut for auto-close
            buttons: {
                confirm: {
                    text: "OK",
                    value: true,
                    visible: true,
                    className: "",
                    closeModal: true
                },
                cancel: {
                    text: "Cancel",
                    value: false,
                    visible: true,
                    className: "",
                    closeModal: true,
                }
            }
        }, function (isConfirm) {
                if (isConfirm) {
                    document.getElementById("registerBtn").innerText = "Try Again";
                    document.getElementById("cancle").style.display = "block"
            } else {
                    swal.close();
                    document.getElementById("registerBtn").innerText = "Try Again";
                    document.getElementById("cancle").style.display = "block"
            }
        });

    }
}

$("#cancle").click(function () {
    if (redirectUrl == "")
        window.location.href = LoginControllerUrl + "/Error?error=access_denied&error_description=user not authenticate"
    else
        window.location.href = redirectUrl + "?error=access_denied&error_description=user not authenticate"
})

function showError(msg) {

    document.getElementById("registerBtn").innerText = "Try Again";
    document.getElementById("cancle").style.display = "block"
    document.getElementById("FIDO2error").classList.remove("hide");
    document.getElementById("FIDO2error").classList.add("show");
    document.getElementById("FIDO2error").innerText = msg;
}

async function verifyAssertionWithServer(assertedCredential, publicKeyOptions) {
    let authData = new Uint8Array(assertedCredential.response.authenticatorData);
    let clientDataJSON = new Uint8Array(assertedCredential.response.clientDataJSON);
    let rawId = new Uint8Array(assertedCredential.rawId);
    let sig = new Uint8Array(assertedCredential.response.signature);
    const FidoDATA = {
        id: assertedCredential.id,
        rawId: coerceToBase64Url(rawId),
        type: assertedCredential.type,
        extensions: assertedCredential.getClientExtensionResults(),
        response: {
            authenticatorData: coerceToBase64Url(authData),
            clientDataJson: coerceToBase64Url(clientDataJSON),
            signature: coerceToBase64Url(sig)
        }
    };


    const data = {
        authenticationScheme: "FIDO2",
        authenticationData: JSON.stringify(FidoDATA)
    }
    let response;
    try {
        let res = await fetch(LoginControllerUrl + "/AuthenticatUser", {
            method: 'POST',
            body: JSON.stringify(data),
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });

        if (!res.ok) {
            response = await res.json();
            throw response;
        } else {
            response = await res.json();
        }
    } catch (e) {

        throw e;
    }

    return response;
}
