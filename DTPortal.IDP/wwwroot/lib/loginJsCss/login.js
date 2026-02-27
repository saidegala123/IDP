var session = "";
var authorizationCode = "";
var sessionID = "";        //authentication session id
var authScheme = [];       //array of authn scheme
var preid = "USER";            //store id of open popup model to close
var voiceFormValue = "";   //store voice form field values
var userName = "";
var errorDivId = "USERerror";
var PushNotificationCode = "";
var codeVerfierInterval = "";
var TimerInterval = "";
var Fido2Options = "";
var QrCode = "";
var client_id = "";
var clientType = "";
var redirectUrl = "";
var state = "";
var ErrorConstant = "";
var LoginControllerUrl = "";
var modal = null;
var selectAuthScheme = null;
var currentAuth = "";
var base64Data = "";
var captured = 0;
var timeoutId;
var detectionid;

var activateSuspendedAccount = null;
var VerifierCode = "";

var mediaStream;
var mediaRecorder;
var recordedChunks = [];
var audioContext;
var analyser;
var audiocanvas;
var audiocanvasnum;
var audiocanvasCtx;
var audiocanvasCtxnum;
var audioPlayback;
var audioPlaybacknum;
var base64audio;

var authenticationScheme = "";
var authenticationMethods = null;

var divElement = document.getElementById('dataId');

// Accessing data attributes using getAttribute()
var method = divElement.getAttribute('data-method');
var target = divElement.getAttribute('data-target');

function closeTimeoutModal() {
    window.location.reload();
    //document.getElementById("requestTimeoutModal").style.display = "none";
    //document.getElementById("requestTimeoutModal").classList.remove("show-modal");
}



function showRetryOrOtherOptions() {
    const htmlContent = `
        <div style="text-align: left;">
            <p style="font-size: 14px;">We sent push notification on your MyTrust mobile app ,but we didn't get response ,Please check your internet connection in mobile and click on the 'Resend notification' button to resend notification.</p>
        </div>
    `;

    swal({
        title: "Request Time Out", // No title
        text: htmlContent,
        html: true,
        showConfirmButton: true,
        confirmButtonText: "Retry",
        showCancelButton: true,
        cancelButtonText: "Try another way",
        closeOnConfirm: false,
        closeOnCancel: false,
        allowOutsideClick: true // allows clicking outside to close
    }, function (isConfirm) {
        if (isConfirm) {
            retryLogin(); // Replace with your retry logic
        } else {
            tryOtherAuth(); // Replace with your alternate auth logic
        }
    });
}


function retryLogin() {
    // Logic to retry the current authentication method
    document.getElementById("requestTimeoutModal").style.display = "none";
    clearInterval(codeVerfierInterval);
    sendCodeAgain();
}

function tryOtherAuth() {
    // Logic to show other authentication options
    document.getElementById("requestTimeoutModal").style.display = "none";
    showOptions(); // Call your custom function that shows auth schemas
}


function getqrcode() {
    try {
        // alert(user_name)
        jQuery.ajax({
            type: "GET",
            url: LoginControllerUrl + "/GetQrCode",
            cache: false,
            beforeSend: function () {
                $('#overlay').show();
            },
            complete: function () {
                $('#overlay').hide();
            },
            error: ErrorHandler,
            success: function (response) {
                document.getElementById('appQrCode').innerHTML = response;
            },
            failure: function () { alert("Failure!!"); }
        });
    } // try
    catch (e) {
        alert(e.message);
    }
}

function showOptions() {

    const schemas = window.authSchemas;
    let htmlContent = `
<div style="text-align:left;">
    <h3 style="font-size:25px; margin-bottom:15px;">Other Sign-In options:</h3>`;

    for (const key in schemas) {
        const schema = schemas[key];

        if (schema) {
            htmlContent += `
<div onclick="changeAuthScheme('${key}')" style="padding:10px; border:1px solid #ddd; border-radius:5px; cursor:pointer; margin:10px 0;">
    <i class="fa ${schema.Icon || ''}" style="margin-right:10px; color:#777;"></i>
    <strong style="font-size:14px;">${schema.Title || ''}</strong><br>
    <small>${schema.Description || ''}</small>
</div>`;
        }
    }

    htmlContent += '</div>';

    swal({
        text: htmlContent,
        title: "",
        html: true,
        showCancelButton: true,
        cancelButtonText: "Cancel",
        confirmButtonText: "Ok"
    }, function () {
        // This triggers when "Close" or Cancel is clicked
        window.location.reload();
    });
}

async function init() {

    //get sp cliet details 
    if (method == "get") {
        var buff = window.atob(target).split("?");
        if (buff[0].startsWith("/Saml2/")) {
            //get Saml2 sp client id
            let buff1 = buff[0].split("/");
            client_id = buff1[3];
            clientType = 2;
        }
        else {
            //get OAuth2 sp client id
            const urlParams = new URLSearchParams(buff[1])
            client_id = urlParams.get('client_id');
            redirectUrl = urlParams.get('redirect_uri');
            state = urlParams.get('state');
            clientType = 1;
        }
    }
    else {
        var buff = window.atob(target);
        var jsonObj = JSON.parse(buff);
        var buff1 = jsonObj.entityEndpoint.split("/");
        client_id = buff1[3];
        clientType = 2;
    }

    LoginControllerUrl = window.location.pathname;
    //attach event
    const checkUser = document.getElementById("checkUser");
    checkUser.addEventListener("click", VerifyUser);

    const checkPwdBtn = document.getElementById("checkPassword");
    checkPwdBtn.addEventListener("click", authenticate);

    const checkTotpBtn = document.getElementById("checkTOTP");
    checkTotpBtn.addEventListener("click", authenticate);

    const checkPinBtn = document.getElementById("checkPin");
    checkPinBtn.addEventListener("click", authenticate);

    //code for registration Guiedline
    modal = document.querySelector(".modal");
    var closeButton = document.querySelector(".close-button");
    var closeButton1 = document.querySelector(".close-btn");
    var cancelButton = document.getElementById('cancelAuthModalBtn');
    var confirmButton = document.getElementById('confirmAuthScheme');
    function toggleModal() {
        modal.classList.toggle("show-modal");
    }
    function AuthModalToggle() {
        clearInterval(codeVerfierInterval);
        deleteCookies();
        authSchemaModal.classList.toggle("show-modal");
    }
    function ConfirmAuthScheme() {
        changeAuthScheme(document.getElementById("authSchemaSelect").value);
    }
    function accountSuspendActivatetoggleModal() {
        activateSuspendedAccount.classList.toggle("show-modal");
    }
    closeButton.addEventListener("click", toggleModal);
    closeButton1.addEventListener("click", toggleModal);
    cancelButton.addEventListener("click", AuthModalToggle);
    confirmButton.addEventListener("click", ConfirmAuthScheme);

    activateSuspendedAccount = document.getElementById("activatesubscriber");
    authenticationMethods = document.getElementById("authenticationmethods");
    var closeButtons = document.querySelector(".cls-btn1");
    var closeButtons1 = document.querySelector(".cls-btn2");
    closeButtons.addEventListener("click", accountSuspendActivatetoggleModal);
    closeButtons1.addEventListener("click", accountSuspendActivatetoggleModal);





    /// new requ time out

    document.getElementById("retryRequest").addEventListener("click", handleRetryModal);
    document.getElementById("tryAnotherWay").addEventListener("click", handleTryAnotherWayModal);
    document.getElementById("closeTimeoutModal").addEventListener("click", handleTimeoutCancelModal);

    function handleRetryModal() {
        document.getElementById("customTimeoutModal").classList.remove("active");
        clearInterval(codeVerfierInterval); // Make sure codeVerfierInterval is defined in your scope
        sendCodeAgain(); // Your custom function to resend the code
    }

    function handleTryAnotherWayModal() {
        // Logic to show other authentication options
        document.getElementById("customTimeoutModal").classList.remove("active");
        showOptions(); // Call your custom function that shows auth schemas
    }

    function handleTimeoutCancelModal() {
        document.getElementById("customTimeoutModal").classList.remove("active");
        window.location.reload();
    }



    // Wrong pin
    document.getElementById("wrongCredentialRetryRequest").addEventListener("click", handleWrongCredentialRetryModal);
    document.getElementById("wrongCredentialTryAnotherWay").addEventListener("click", handleWrongCredentialTryAnotherWayModal);
    document.getElementById("wrongCredentialCloseTimeoutModal").addEventListener("click", handleWrongCredentialTimeoutCancelModal);

    function handleWrongCredentialRetryModal() {
        document.getElementById("wrongCredentialModal").classList.remove("active");
        clearInterval(codeVerfierInterval); // Ensure this variable is defined in your scope
        sendCodeAgain(); // Your custom function to resend the code
    }

    function handleWrongCredentialTryAnotherWayModal() {
        // Logic to show other authentication options
        document.getElementById("wrongCredentialModal").classList.remove("active");
        showOptions(); // Your custom function that shows auth schemas
    }

    function handleWrongCredentialTimeoutCancelModal() {
        document.getElementById("wrongCredentialModal").classList.remove("active");
        window.location.reload();
    }


    //function accountSuspendActivatetoggleModal() {
    //    activateSuspendedAccount.classList.toggle("show-modal");
    //}

    // Loop through all close buttons and add click event listeners
    //closeButtons.forEach(function (button) {
    //    button.addEventListener("click", accountSuspendActivatetoggleModal);
    //});

    //closeButtons1.forEach(function (button) {
    //    button.addEventListener("click", accountSuspendActivatetoggleModal);
    //});
    closeButton.addEventListener("click", toggleModal);
    closeButton1.addEventListener("click", toggleModal);

    deleteCookies();
    $('#networkOverlay').hide();
    $('#authSchemaSelect').select2({
        width: '100%',
        placeholder: "Select an option",
        dropdownParent: $('#authSchemaModal')
    });
}

//$(".toggle-password").click(function () {
//    $(this).toggleClass("fa-eye fa-eye-slash");
//    var input = $($(this).attr("toggle"));
//    if (input.attr("type") == "password") {
//        input.attr("type", "text");
//    } else {
//        input.attr("type", "password");
//    }
//});

$(".toggle-password").click(function () {
    $(this).toggleClass("fa-eye fa-eye-slash");
    var input = $($(this).attr("toggle"));
    if (input.hasClass("pwd")) {
        input.removeClass("pwd");
    } else {
        input.addClass("pwd");
    }
});

$("input").keypress(function (event) {
    if (event.which == 13) {
        event.preventDefault();
        if (this.form.id == "UserForm")
            VerifyUser();
        else
            authenticate(this.form.id);
    }
});

//funtion for manage div flow
function show() {

    var id = authScheme.pop();
    currentAuth = id;
    if (id == undefined) {

        //submit form after complete authentication
        $('#overlay').hide();
        $(':button').prop('disabled', true);
        var url = LoginControllerUrl + window.location.search
        postForm(url, { username: userName, target: target, method: method });

    } else {

        //set voice phrase value
        if (id == "VOICE_TI" || id == "VOICE_DIGIT_RECOGNISATION" ||
            id == "VOICE_TPD_RECOGNISATION") {
            setElementValues(id);
            id = "VOICE";
        }

        //set push notification code
        if (id == "PUSH_NOTIFICATION") {
            document.getElementById("PushNotificationCode").innerText = PushNotificationCode;
            setCookies();
            codeVerfierInterval = setTimeout(isUserVerifiedCode, 500);
            startTimer1();
            //isUserVerifiedCode();
        }

        //set qr code
        if (id == "QRCODE" || id == "WALLET") {
            id = "QRCODE";
            //var qrImage = "<img src='" + QrCode + "' class='qr-code img-thumbnail img-responsive'></img>";

            var imgElement = document.createElement("img");
            imgElement.src = QrCode;  // Set image source
            imgElement.classList.add("qr-code", "img-thumbnail", "img-responsive");

            // Get the container and insert the image
            document.getElementById("QRCODEdata").innerHTML = "";
            document.getElementById("QRCODEdata").appendChild(imgElement);


            //document.getElementById("QRCODEdata").innerText = qrImage;
            setCookies();
            isUserVerifiedWalletQRCode();
            //if (id == "QRCODE") {
            //    codeVerfierInterval = setTimeout(isUserVerifiedQRCode, 20000);
            //}
            //else {
            //    isUserVerifiedWalletQRCode();
            //}

            //startTimer();
            //isUserVerifiedCode();
        }

        if (id == "MOBILE_TOTP") {
            setCookies();
            codeVerfierInterval = setTimeout(isUserVerifiedCode, 500);
        }
        //close previous window
        if (preid) {
            var div1 = $("#" + preid)
            div1.css("display", "none");
        }
        if (id === "WEB_FACE") {

            Promise.all([
                faceapi.nets.tinyFaceDetector.loadFromUri('./models'),
                faceapi.nets.faceLandmark68Net.loadFromUri('./models'),
                faceapi.nets.faceRecognitionNet.loadFromUri('./models'),
                faceapi.nets.faceExpressionNet.loadFromUri('./models')

            ]).then(startVideo)
        }
        //var auth = id ; 
        id = id.toUpperCase();
        errorDivId = id + "error";
        //set user name to new  window
        var userNameid = id + "userName";
        if (id != "WEB_FACE" && id != "VOICE") {
            document.getElementById(userNameid).value = userName;
        }
        //set new id to preid
        preid = id;
        var div = $("#" + id)
        if (div.length) {
            div.css("display", "block");

            //call FIDO2 function
            if (id == "FIDO2") {
                document.getElementById("FIDOOption").value = Fido2Options;
                setTimeout(handleSignInSubmit(Fido2Options), 3000);
                /*handleSignInSubmit(Fido2Options)*/
                return;
            }
        }
        else
            swal("Error", "The " + id + " Authentication Scheme is not available")
    }
}

function ValidateNumber(userName) {
    if (userName.startsWith("+256")) {
        if (userName.length != 13) {
            return null;
        }
    } else if (userName.startsWith("256")) {
        if (userName.length != 12) {
            return null;
        }
        userName = "+" + userName;

    } else if (userName.startsWith("+91")) {
        if (userName.length != 13) {
            return null;
        }
    }
    else if (userName.startsWith("91")) {
        if (userName.length != 12) {
            return null;
        }
        userName = "+" + userName;
    }
    else if (userName.startsWith("0")) {
        if (userName.length == 10) {
            userName = userName.replace("0", "+256");
        }
        else if (userName.length == 11) {
            userName = userName.replace("0", "+91");
        } else {
            return null;
        }
    }
    else if (userName.length == 9) {
        userName = "+256" + userName;
    }

    else if (userName.length == 10) {
        userName = "+91" + userName;
    }
    else {
        return null
    }

    return userName
}
//function for validate user input 
function VerifyUser() {

    //verify coockies enable on browser
    if (!navigator.cookieEnabled) {
        document.getElementById("browserErrordiv").style.display = "flex"
        document.getElementById("browserError").innerHTML = "Your browser cookies option is disabled.\ Please enable cookies option \n Go browser setting -> Cookies and other site data -> Enable 'Allow all cookies'!";
        return false;
    }

    var form = new FormData(document.getElementById("UserForm"));
    userName = form.get("username");
    userName = userName.trim();
    if (!userName) {

        var element = document.getElementById("username");
        element.classList.add("invalid");
        document.getElementById("username").style.boxShadow = "0px 10px 10px -1px #bfd01f";
        document.getElementById("USERerror").style.visibility = "visible"
        document.getElementById("USERerror").className = "danger";
        document.getElementById("USERerror").innerHTML = "Please enter the value";
        return true;

    } else {

        var type = getInputType(userName);
        if (type == 1) {

            userName = ValidateNumber(userName)
            if (userName == null) {
                var element = document.getElementById("username");
                element.classList.add("invalid");
                document.getElementById("username").style.boxShadow = "0px 10px 10px -1px #bfd01f";
                document.getElementById("USERerror").style.visibility = "visible"
                document.getElementById("USERerror").className = "danger";
                document.getElementById("USERerror").innerHTML = "Invalid Mobile Number";
                return true;
            }
        }

        var userAgent = navigator.userAgent;
        var typeOfDevice = getDeviceType();

        $.ajax({
            type: 'POST',
            url: LoginControllerUrl + "/VerifyUser",
            dataType: 'json',
            data: {
                userInput: userName,
                type: type,
                clientId: client_id,
                clientType: clientType,
                userAgent: userAgent,
                typeOfDevice: typeOfDevice
            },
            beforeSend: showLoder,
            complete: hideLoder,
            error: ErrorHandler,
            success: function (result, status, xhr) {

                if (document.getElementById("USERerror").style.visibility == "visible") {
                    document.getElementById("USERerror").style.visibility = "hidden"
                    document.getElementById("USERerror").className = "info"
                }
                if (result.success) {
                    userName = result.result.userName
                    if (result.result.mobileUser) {
                        authenticationmethods.classList.toggle("show-modal");
                    }
                    else {
                        //  sessionID = result.data.temporarySession;
                        result.result.authenticationSchemes.forEach(element => {
                            if (element == "PUSH_NOTIFICATION") {
                                PushNotificationCode = result.result.randomCode;//get pushnotification code
                            }
                            if (element == "FIDO2") {
                                Fido2Options = result.result.fido2Options;//get pushnotification code
                            }
                            if (element == "QRCODE" || element == "WALLET") {
                                QrCode = result.result.qrCode;//get qr code
                                if (element == "WALLET") {
                                    VerifierCode = result.result.verifierCode;
                                }
                            }
                            authScheme.push(element);
                        });
                        authScheme.reverse();

                        // modal.style.display = 'block';
                        show();
                    }

                } else {

                    if (result.errorCode == 100) {
                        swal({ type: 'info', title: "Invalid Client", text: result.message }, function (isConfirm) {
                            if (isConfirm) {
                                clearInterval(codeVerfierInterval);
                                deleteCookies();
                                if (redirectUrl == "")
                                    window.location.href = LoginControllerUrl + "/Error?error=Invalid Client&error_description=" + result.message
                                else
                                    window.location.href = redirectUrl + "?error=Invalid Client&error_description=" + result.message + "&state=" + state
                            }
                        });
                    }
                    else if (result.errorCode == 101) {
                        swal({ type: 'info', title: "Inactive Client", text: result.message }, function (isConfirm) {
                            if (isConfirm) {
                                clearInterval(codeVerfierInterval);
                                deleteCookies();
                                if (redirectUrl == "")
                                    window.location.href = LoginControllerUrl + "/Error?error=Inactive Client&error_description=" + result.message
                                else
                                    window.location.href = redirectUrl + "?error=Inactive Client&error_description=" + result.message + "&state=" + state
                            }
                        });
                    }
                    else if (result.errorCode == 103) {
                        if (client_id != "ONBN53C1ydOLfvrJllgxuj9PwcyrpR5aOg5idWxnEXXwSqFe") {
                            swal({
                                title: result.message,
                                text: "Your Account is Suspended for " + result.accountlocktime + " hours ,Please contact Administrator ",
                                button: "Close", // Text on button
                                icon: "info", //built in icons: success, warning, error, info
                                showCancelButton: true,
                                confirmButtonText: "Self Activation Guidelines",
                                cancelButtonText: "Cancel",
                                closeOnConfirm: true,
                                closeOnCancel: true
                            }, function (isConfirm) {
                                if (isConfirm) {
                                    activateSuspendedAccount.classList.toggle("show-modal");
                                } else {
                                    swal.close();
                                }
                            });
                        }
                        else {
                            swal({ type: 'info', title: "Account Suspended", text: result.message }, function (isConfirm) {
                                if (isConfirm) {
                                    clearInterval(codeVerfierInterval);
                                    deleteCookies();
                                    //if (redirectUrl == "")
                                    //    window.location.href = LoginControllerUrl + "/Error?error=Account Suspended&error_description=" + result.message
                                    //else
                                    //    window.location.href = redirectUrl + "?error=Account Suspended&error_description=" + result.message + "&state=" + state
                                }
                            });
                        }
                    }

                    else if (result.errorCode == 104) {
                        swal({ type: 'info', title: "Inactive Account", text: result.message }, function (isConfirm) {
                            if (isConfirm) {
                                clearInterval(codeVerfierInterval);
                                deleteCookies();
                                //if (redirectUrl == "")
                                //    window.location.href = LoginControllerUrl + "/Error?error=Inactive Account&error_description=" + result.message
                                //else
                                //    window.location.href = redirectUrl + "?error=Inactive Account&error_description=" + result.message + "&state=" + state
                            }
                        });
                    }
                    else if (result.errorCode == 105) {

                        // wrongCredential
                        //document.getElementById("wrongCredentialModal").classList.add("active");

                        // time Out
                        //document.getElementById("customTimeoutModal").classList.add("active");

                        swal({
                            type: 'info',
                            title: result.message,
                            text: "Click on a button to proceed",
                            showCancelButton: true,
                            confirmButtonText: "Ok",
                            cancelButtonText: "Try Another Way",
                            closeOnConfirm: false,
                            closeOnCancel: false
                        }, function (isConfirm) {
                            if (isConfirm) {
                                clearInterval(codeVerfierInterval);
                                deleteCookies();
                                window.location.reload();
                            }
                            else {
                                //swal.close();

                                showOptions();

                                //showRetryOrOtherOptions();

                                //authSchemaModal.classList.toggle("show-modal");
                            }
                        });

                    }
                    else {
                        if (result.errorCode == 102 && client_id != "ONBN53C1ydOLfvrJllgxuj9PwcyrpR5aOg5idWxnEXXwSqFe") {
                            swal({
                                title: result.message,
                                text: "Do you want to see registration guidelines",
                                button: "Close", // Text on button
                                icon: "info", //built in icons: success, warning, error, info
                                showCancelButton: true,
                                confirmButtonText: "Registration Guidelines",
                                cancelButtonText: "Cancel",
                                closeOnConfirm: true,
                                closeOnCancel: true
                            }, function (isConfirm) {
                                if (isConfirm) {
                                    modal.classList.toggle("show-modal");
                                } else {
                                    swal.close();
                                }
                            });

                        } else {
                            document.getElementById("USERerror").className = "danger";
                            document.getElementById("USERerror").style.visibility = "visible"
                            document.getElementById("USERerror").innerHTML = result.message;
                        }


                    }
                    return false;

                }
            }
        })
    }
}


// function to change Auth Schema
function changeAuthScheme(authenticationScheme) {
    swal.close();
    $.ajax({
        url: LoginControllerUrl + "/ChangeAuthScheme",
        type: 'GET',
        data: { authScheme: authenticationScheme },
        beforeSend: showLoder,
        complete: hideLoder,
        error: ErrorHandler,
        success: function (response) {
            authScheme = [];
            response.result.authenticationSchemes.forEach(element => {
                if (element == "WALLET") {
                    QrCode = response.result.qrCode;
                    if (element == "WALLET") {
                        VerifierCode = response.result.verifierCode;
                    }
                }
                authScheme.push(element);
            });
            authScheme.reverse();

            // modal.style.display = 'block';
            show();
        }
    });
}
function changeAssistedAuthScheme(authenticationScheme) {
    $.ajax({
        url: LoginControllerUrl + "/ChangeAuthScheme",
        type: 'GET',
        data: { authScheme: authenticationScheme },
        beforeSend: showLoder,
        complete: hideLoder,
        error: ErrorHandler,
        success: function (response) {
            authScheme = [];
            response.result.authenticationSchemes.forEach(element => {
                if (element == "WALLET") {
                    QrCode = response.result.qrCode;
                    if (element == "WALLET") {
                        VerifierCode = response.result.verifierCode;
                    }
                }
                authScheme.push(element);
            });
            authScheme.reverse();

            // modal.style.display = 'block';
            show();
        }
    });
}

//function for validate user authentication data
function authenticate(FormID) {

    if (!navigator.cookieEnabled) {
        document.getElementById("browserErrordiv").style.display = "flex"
        document.getElementById("browserError").innerHTML = "Your browser cookies option is disabled.\ Please enable cookies option \n Go browser setting -> Cookies and other site data -> Enable 'Allow all cookies'!";
        return false;
    }

    var formName = ""
    if (typeof FormID == "string")
        formName = FormID;
    else
        formName = $(this).parents("form").attr("id");

    var form = new FormData(document.getElementById(formName));
    var password = form.get("password");

    if (!password) {

        var id = form.get("authScheme").toLowerCase();
        var element = document.getElementById(id);
        element.classList.add("invalid");
        document.getElementById("password").style.boxShadow = "0px 10px 10px -1px #bfd01f";
        document.getElementById(errorDivId).className = "danger";
        document.getElementById(errorDivId).style.visibility = "visible";
        var msg = (id != "email") ? id : id + " otp";
        document.getElementById(errorDivId).innerHTML = "Please enter " + msg
        return false;

    } else {
        $.ajax({
            type: 'POST',
            url: LoginControllerUrl + "/AuthenticatUser",
            contentType: 'application/json',
            dataType: 'json',
            data: JSON.stringify({
                authenticationScheme: form.get("authScheme"),
                authenticationData: form.get("password")
            }),
            beforeSend: showLoder,
            complete: hideLoder,
            error: ErrorHandler,
            success: function (result, status, xhr) {
                if (result.success) {
                    if (result && result.result && result.result.randomCode) {
                        PushNotificationCode = result.result.randomCode;
                    }
                    if (document.getElementById(errorDivId).style.visibility == "visible") {
                        document.getElementById(errorDivId).style.visibility = "hidden"
                        document.getElementById(errorDivId).className = "info"
                    }

                    show();
                }
                else {
                    document.getElementById("password").value = "";
                    if (result.errorCode == 106) {
                        swal({ type: 'info', title: result.message, text: "Click on 'Ok' button for login again!" }, function (isConfirm) {
                            if (isConfirm) {
                                deleteCookies();
                                window.location.reload();
                            }
                        });
                    }
                    else if (result.errorCode == 103) {
                        swal({ type: 'info', title: "Account Suspended", text: result.message }, function (isConfirm) {
                            if (isConfirm) {
                                clearInterval(codeVerfierInterval);
                                deleteCookies();
                                window.location.reload();
                                //if (redirectUrl == "")
                                //    window.location.href = LoginControllerUrl + "/Error?error=Account Suspended&error_description=" + result.message
                                //else
                                //    window.location.href = redirectUrl + "?error=Account Suspended&error_description=" + result.message + "&state=" + state
                            }
                        });
                    } else {
                        document.getElementById(errorDivId).className = "danger";
                        document.getElementById(errorDivId).style.visibility = "visible"
                        document.getElementById(errorDivId).innerHTML = result.message;
                    }
                    return false;
                }
            }

        })
    }
}

//function for check user is verify pushnotification code on app 
function isUserVerifiedCode() {

    if (!navigator.cookieEnabled) {
        document.getElementById("browserErrordiv").style.display = "flex"
        document.getElementById("browserError").innerHTML = "Your browser cookies option is disabled.\ Please enable cookies option \n Go browser setting -> Cookies and other site data -> Enable 'Allow all cookies'!";
        return false;
    }

    $.ajax({
        type: 'post',
        url: LoginControllerUrl + "/IsUserVerifiedCode",
        success: function (result, status, xhr) {
            if (result.success && result.status == "success") {
                clearInterval(codeVerfierInterval);
                stopTimer();
                deleteCookies();
                show();
            }
            else {

                if (!result.success && result.status == "failed") {

                    stopTimer();

                    if (result.errorCode == 107 || result.errorCode == 106) {
                        swal({ type: 'info', title: result.message, text: "Click on 'Ok' button for login again!" }, function (isConfirm) {
                            if (isConfirm) {
                                clearInterval(codeVerfierInterval);
                                deleteCookies();
                                window.location.reload();
                            }
                        });
                    } else {
                        document.getElementById("wrongCredentialModal").classList.add("active");
                        //swal({
                        //    title: result.message,
                        //    text: `Do you want to reauthenticate!\nPlease click on the 'Resend notification' button\n to resend notification.`,
                        //    type: "error",
                        //    showCancelButton: true,
                        //    //  confirmButtonColor: "#DD6B55",
                        //    confirmButtonText: "Resend Notification",
                        //    cancelButtonText: "Cancel",
                        //    closeOnConfirm: false,
                        //    closeOnCancel: true
                        //}, function (isConfirm) {
                        //    if (isConfirm) {
                        //        clearInterval(codeVerfierInterval);
                        //        sendCodeAgain();
                        //    }
                        //    else {
                        //        clearInterval(codeVerfierInterval);
                        //        deleteCookies();
                        //        if (redirectUrl == "")
                        //            window.location.href = LoginControllerUrl + "/Error?error=access_denied&error_description=user not authenticated"
                        //        else
                        //            window.location.href = redirectUrl + "?error=access_denied&error_description=user not authenticated&state=" + state
                        //        //window.history.back();
                        //        //window.location.reload();
                        //    }
                        //});

                    }
                }
                if (result.status == "stop") {

                    clearInterval(codeVerfierInterval);
                    stopTimer();
                    //showRetryOrOtherOptions();

                    document.getElementById("customTimeoutModal").classList.add("active");

                    //swal({
                    //    title: "Request Time Out",
                    //    html: true,
                    //    text: `We sent push notification on your MyTrust mobile app ,<br/>but we didn't get response ,Please check your internet connection in mobile and click on the 'Resend notification' button to resend notification.`,
                    //    type: "error",
                    //    showCancelButton: true,
                    //    //  confirmButtonColor: "#DD6B55",
                    //    confirmButtonText: "Resend Notification",
                    //    cancelButtonText: "Cancel",
                    //    closeOnConfirm: false,
                    //    closeOnCancel: true
                    //}, function (isConfirm) {
                    //    if (isConfirm) {
                    //        clearInterval(codeVerfierInterval);
                    //        sendCodeAgain();
                    //    }
                    //    else {
                    //        clearInterval(codeVerfierInterval);
                    //        deleteCookies();
                    //        if (redirectUrl == "")
                    //            window.location.href = LoginControllerUrl + "/Error?error=access_denied&error_description=user not authenticated"
                    //        else
                    //            window.location.href = redirectUrl + "?error=access_denied&error_description=user not authenticated&state=" + state
                    //        //window.history.back();
                    //        //window.location.reload();
                    //    }
                    //});
                }
            }
        },
        complete: function (data) {
            if (data.status == 200 && data.responseJSON.success && data.responseJSON.status == "pending") {
                // if (data.responseJSON.operation != "stop")
                codeVerfierInterval = setTimeout(isUserVerifiedCode, 500);
            }
        },
        error: ErrorHandler
    })

};

//function for check user is verify QR code on app 
function isUserVerifiedQRCode() {
    if (!navigator.cookieEnabled) {
        document.getElementById("browserErrordiv").style.display = "flex";
        document.getElementById("browserError").innerHTML = "Your browser cookies option is disabled. Please enable cookies option \n Go browser setting -> Cookies and other site data -> Enable 'Allow all cookies'!";
        return false;
    }

    $.ajax({
        type: 'post',
        url: LoginControllerUrl + "/IsUserVerifiedQRCode",
        success: function (result, status, xhr) {
            if (result.success && result.status == "success") {
                //clearInterval(codeVerfierInterval);
                //stopTimer();
                deleteCookies();
                show();
            } else {
                if (!result.success && result.status == "failed") {
                    //stopTimer();
                    if (result.errorCode == 107 || result.errorCode == 106) {
                        swal(
                            { type: 'info', title: result.message, text: "Click on 'Ok' button for login again!" },
                            function (isConfirm) {
                                if (isConfirm) {
                                    //clearInterval(codeVerfierInterval);
                                    deleteCookies();
                                    window.location.reload();
                                }
                            }
                        );
                    }
                }
            }
        },
        complete: function (data) {
            if (data.status == 200 && data.responseJSON.success && data.responseJSON.status == "pending") {
                // if (data.responseJSON.operation != "stop")
                codeVerfierInterval = setTimeout(isUserVerifiedQRCode, 500);
            }
        },
        error: ErrorHandler
    });
}


function isUserVerifiedWalletQRCode() {
    if (!navigator.cookieEnabled) {
        document.getElementById("browserErrordiv").style.display = "flex";
        document.getElementById("browserError").innerHTML = "Your browser cookies option is disabled. Please enable cookies option \n Go browser setting -> Cookies and other site data -> Enable 'Allow all cookies'!";
        return false;
    }

    $.ajax({
        type: 'post',
        url: LoginControllerUrl + "/IsUserVerified",
        success: function (result, status, xhr) {
            if (result && result.success) {
                if (result.result && result.result.randomCode) {
                    PushNotificationCode = result.result.randomCode;
                }
                show();
            } else {
                swal(
                    {
                        type: 'info', title: result.message,
                        text: "Do you want to reauthenticate!\nPlease click on the Rescan",
                        showCancelButton: true,
                        confirmButtonText: "Rescan",
                        cancelButtonText: "Cancel",
                        closeOnConfirm: true,
                        closeOnCancel: true
                    },
                    function (isConfirm) {
                        if (isConfirm) {
                            generateQrCodeAgain();
                        }
                        else {
                            deleteCookies();
                            if (redirectUrl == "")
                                window.location.href = LoginControllerUrl + "/Error?error=access_denied&error_description=user not authenticated"
                            else
                                window.location.href = redirectUrl + "?error=access_denied&error_description=user not authenticated&state=" + state
                        }
                    }
                );
            }
        },
        error: ErrorHandler
    });
}


//function for send pushnotification code again
function sendCodeAgain() {

    if (!navigator.cookieEnabled) {
        document.getElementById("browserErrordiv").style.display = "flex"
        document.getElementById("browserError").innerHTML = "Your browser cookies option is disabled.\ Please enable cookies option \n Go browser setting -> Cookies and other site data -> Enable 'Allow all cookies'!";
        return false;
    }

    $.ajax({
        type: 'post',
        url: LoginControllerUrl + "/SendPushNotification",
        beforeSend: showLoder,
        complete: hideLoder,
        error: ErrorHandler,
        success: function (result, status, xhr) {
            if (result.success) {

                if (result.errorCode == 108) {

                    swal({
                        title: "Success!",
                        text: "You are already AUTHENTICATED!",
                        button: "Close", // Text on button
                        icon: "success", //built in icons: success, warning, error, info
                        timer: 5000, //timeOut for auto-close
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
                            clearInterval(codeVerfierInterval);
                            deleteCookies();
                            show();
                        } else {
                            swal.close();
                            clearInterval(codeVerfierInterval);
                            deleteCookies();
                            show();
                        }
                    });


                } else {

                    swal({
                        title: "Success!",
                        text: "Code Send Successfully!",
                        timer: 2000,
                        type: "success",
                        showConfirmButton: false
                    });

                    if (document.getElementById(errorDivId).style.visibility == "visible") {
                        document.getElementById(errorDivId).className = "info";
                        document.getElementById(errorDivId).style.visibility = "hidden"
                        document.getElementById(errorDivId).innerHTML = "";
                    }
                    document.getElementById("PushNotificationCode").innerText = result.randomCode;
                    codeVerfierInterval = setTimeout(isUserVerifiedCode, 500);
                    setCookies();
                    startTimer1();
                    // isUserVerifiedCode();
                }
            }
            else {

                if (result.errorCode == 107 || result.errorCode == 106) {

                    swal({
                        title: result.message,
                        text: "Click on 'Ok' button for login again!",
                        button: "Close", // Text on button
                        icon: "info", //built in icons: success, warning, error, info
                        timer: 10000, //timeOut for auto-close
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
                            deleteCookies();
                            window.location.reload();
                        } else {
                            swal.close();
                            deleteCookies();
                            window.location.reload();
                        }
                    });
                }
                else if (result.errorCode == 103) {

                    swal({
                        title: "Account Suspended",
                        text: result.message,
                        button: "Close", // Text on button
                        icon: "info", //built in icons: success, warning, error, info
                        timer: 10000, //timeOut for auto-close
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
                            clearInterval(codeVerfierInterval);
                            deleteCookies();
                            if (redirectUrl == "")
                                window.location.href = LoginControllerUrl + "/Error?error=account_suspended&error_description=" + result.message
                            else
                                window.location.href = redirectUrl + "?error=account_suspended&error_description=" + result.message + "&state=" + state
                        } else {
                            swal.close();
                            clearInterval(codeVerfierInterval);
                            deleteCookies();
                            if (redirectUrl == "")
                                window.location.href = LoginControllerUrl + "/Error?error=account_suspended&error_description=" + result.message
                            else
                                window.location.href = redirectUrl + "?error=account_suspended&error_description=" + result.message + "&state=" + state
                        }
                    });
                }
                else {
                    swal({
                        title: result.message,
                        text: `Do you want to reauthenticate!\nPlease click on the 'Resend notification' button\n to resend notification.`,
                        type: "error",
                        showCancelButton: true,
                        // confirmButtonColor: "#DD6B55",
                        confirmButtonText: "Resend Notification",
                        cancelButtonText: "Cancel",
                        closeOnConfirm: false,
                        closeOnCancel: true
                    }, function (isConfirm) {
                        if (isConfirm) {
                            sendCodeAgain();
                        }
                        else {
                            clearInterval(codeVerfierInterval);

                            deleteCookies();
                            if (redirectUrl == "")
                                window.location.href = LoginControllerUrl + "/Error?error=access_denied&error_description=user not authenticated"
                            else
                                window.location.href = redirectUrl + "?error=access_denied&error_description=user not authenticated&state=" + state
                            //window.history.back();
                            //window.location.reload();
                        }
                    });
                }
            }
        }

    })
}

function generateQrCodeAgain() {

    if (!navigator.cookieEnabled) {
        document.getElementById("browserErrordiv").style.display = "flex"
        document.getElementById("browserError").innerHTML = "Your browser cookies option is disabled.\ Please enable cookies option \n Go browser setting -> Cookies and other site data -> Enable 'Allow all cookies'!";
        return false;
    }

    $.ajax({
        type: 'get',
        url: LoginControllerUrl + "/GenerateQrCode",
        beforeSend: showLoder,
        complete: hideLoder,
        error: ErrorHandler,
        success: function (result, status, xhr) {
            if (result.success) {
                var imgElement = document.createElement("img");
                imgElement.src = result.result;
                imgElement.classList.add("qr-code", "img-thumbnail", "img-responsive");
                document.getElementById("QRCODEdata").innerHTML = "";
                document.getElementById("QRCODEdata").appendChild(imgElement);
                isUserVerifiedWalletQRCode();
            }
            else {
                swal(
                    {
                        type: 'info', title: result.message,
                        text: "Do you want to reauthenticate!\nPlease click on the Rescan",
                        showCancelButton: true,
                        confirmButtonText: "Rescan",
                        cancelButtonText: "Cancel",
                        closeOnConfirm: true,
                        closeOnCancel: true
                    },
                    function (isConfirm) {
                        if (isConfirm) {
                            generateQrCodeAgain();
                        }
                        else {
                            deleteCookies();
                            if (redirectUrl == "")
                                window.location.href = LoginControllerUrl + "/Error?error=access_denied&error_description=user not authenticated"
                            else
                                window.location.href = redirectUrl + "?error=access_denied&error_description=user not authenticated&state=" + state
                        }
                    }
                );
            }
        },
        error: ErrorHandler
    })
}

function showLoder() {
    $('#overlay').show();
}
function hideLoder() {
    $('#overlay').hide();
}
function ErrorHandler(xhr, status, error) {

    switch (xhr.status) {
        case 400:
            document.getElementById(errorDivId).className = "danger";
            document.getElementById(errorDivId).style.visibility = "visible"
            document.getElementById(errorDivId).innerHTML = xhr.responseJSON.message
            break;
        case 403:
            swal("Forbidden", "You do not have access to this resource", "error");
            break;
        case 404:
            swal("Abort", "The resource you requested could not be found", "error");
            break;

        case 500:
            swal({ title: "Server Error", text: "Internal Server error Occurred", type: "error" }, function (isConfirm) {
                if (isConfirm) {
                    deleteCookies();
                    window.location.reload();
                }
            });
            break;

        case 502:
            swal("Bad Gateway", "Invalid response", "error");
            break;
        case 503:
            swal("Service unavailable", "The Service is currently unavailable, please try after sometime.", "error");
            break;
        default:
            errors = ["Bad Request", "Bad Gateway", "Not Found", "Internal Server Error", "Forbidden", "Unauthorized", "Service Unavailable"]
            if (xhr.readyState == 0 && xhr.status == 0 && xhr.responseJSON == undefined) {
                if (!errors.includes(error)) {

                    if (window.navigator.onLine) {
                        if (error == "") {
                            error = "Something went wrong, Please Try later";
                        }

                        swal({ type: 'error', title: "Server Down", text: error }, function (isConfirm) {
                            if (isConfirm) {
                                deleteCookies();
                                window.location.reload();
                            }
                        });
                    } else {
                        swal({ type: 'info', title: "No Internet", text: "Check your network connection!" }, function (isConfirm) {
                            if (isConfirm) {
                                deleteCookies();
                                window.location.reload();
                            }
                        });
                    }
                } else {
                    swal({
                        type: 'error', title: "Error", text: "Something went wrong, please try later"
                    }, function (isConfirm) {
                        if (isConfirm) {
                            window.location.reload();
                        }
                    });
                }
            }
            break;
    }
}

//function for submit user data after authentication success
function postForm(path, params, method) {
    clearInterval(codeVerfierInterval);

    method = method || 'post';

    var form = document.createElement('form');
    form.setAttribute('method', method);
    form.setAttribute('action', path);

    for (var key in params) {
        if (params.hasOwnProperty(key)) {
            var hiddenField = document.createElement('input');
            hiddenField.setAttribute('type', 'hidden');
            hiddenField.setAttribute('name', key);
            hiddenField.setAttribute('value', params[key]);

            form.appendChild(hiddenField);
        }
    }

    document.body.appendChild(form);
    form.submit();
    $('#overlay1').show();
}

function ForgotPassword() {

    var username = document.getElementById("PASSWORDuserName").value;
    var userType = getInputType(username);
    window.location.href = LoginControllerUrl + "/ForgotPassword/" + username + "?UserType=" + userType
}
//function for verify user input type
function getInputType(userName) {
    var length = userName.length;

    // Email
    if (/^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$/.test(userName))
        return 2;

    // India (+91)
    else if (/^(\+91|91|0)?[6-9]\d{9}$/.test(userName))
        return 1;

    // Uganda (+256)
    else if (/^(\+256|256|0)?\d{9}$/.test(userName))
        return 1;

    // UAE (+971)
    else if (/^(\+971|971|0)?5\d{8}$/.test(userName))
        return 1;

    // Emirates ID
    else if (/^784-?\d{4}-?\d{7}-?\d$/.test(userName))
        return 6;

    // 14-character alphanumeric
    else if (length === 14 && /^[A-Z0-9]{14}$/.test(userName))
        return 3;

    else if (/^[A-Z]{1,4}[0-9]{5,16}$/.test(userName))
        return 4;

    else
        return 5;
}


//function for getting user device type used for login
function getDeviceType() {
    const ua = navigator.userAgent;
    if (/(tablet|ipad|playbook|silk)|(android(?!.*mobi))/i.test(ua)) {
        return "tablet";
    }
    if (
        /Mobile|iP(hone|od)|Android|BlackBerry|IEMobile|Kindle|Silk-Accelerated|(hpw|web)OS|Opera M(obi|ini)/.test(
            ua
        )
    ) {
        return "mobile";
    }
    return "desktop";
};


function setCookies() {
    var expires = (new Date(Date.now() + 60 * 1000)).toGMTString();
    document.cookie = "NotificationVerifierValidTime=yes; expires=" + expires + ";path=/;"
}

function deleteCookies() {

    document.cookie = "NotificationVerifierValidTime=yes; expires=" + new Date(0).toUTCString();

    //var allCookies = document.cookie.split(';');
    //// The "expire" attribute of every cookie is  
    //// Set to "Thu, 01 Jan 1970 00:00:00 GMT" 
    //for (var i = 0; i < allCookies.length; i++) {
    //    if (!allCookies[i].startsWith("IDPCookieConsent=yes"))
    //        document.cookie = allCookies[i] + "=;expires=" + new Date(0).toUTCString();
    //}
}

function startTimer1() {
    var timerdiv = document.getElementById("timerDiv");
    var display = document.getElementById('timer');

    timerdiv.style.visibility = "visible";
    var timer = 60;

    TimerInterval = setInterval(function () {

        display.textContent = (timer >= 10) ? timer : "0" + timer;

        if (--timer < -1) {
            timerdiv.style.visibility = "hidden";
            display.textContent = "";
            clearInterval(TimerInterval);
        }
    }, 1000);
}

function stopTimer() {
    document.getElementById("timerDiv").style.visibility = "hidden";
    document.getElementById('timer').textContent = "";
    clearInterval(TimerInterval);
}
function startVideo() {

    navigator.getUserMedia(
        { video: {} },
        stream => {
            video.srcObject = stream;
            videoStream = stream; // Store the video stream for later use
        },
        err => {
            console.error(err)
            if (err.name === 'NotFoundError' || err.name === 'DevicesNotFoundError') {

                swal({ type: 'info', title: "Camera Not Found", text: 'No camera device found.' }, function (isConfirm) {
                    if (isConfirm) {

                    }
                });

            } else if (err.name === 'NotAllowedError' || err.name === 'PermissionDeniedError') {

                swal({ type: 'info', title: "Camera Permission", text: 'Permission to access camera was denied.' }, function (isConfirm) {
                    if (isConfirm) {

                    }
                });
            }
            else {
                alert();
                swal({ type: 'info', title: "Camera Error", text: 'Error accessing camera: ' + err.message }, function (isConfirm) {
                    if (isConfirm) {

                    }
                });
            }
        }
    );

    document.getElementById('video').style.display = 'block';
    timeoutId = setTimeout(() => {
        clearInterval(detectionid);
        if (videoStream) {
            videoStream.getTracks().forEach(track => track.stop()); // Stop all tracks of the video stream

        }
        video.style.display = 'none';

        setTimeout(() => {
            document.getElementById('faceinstructions').style.display = 'none';
            document.getElementById('camblock').style.display = 'block';
            document.getElementById('recapturediv').style.display = 'block';
        }, 100)




    }, 20000);

}

function stopVideo() {
    if (videoStream) {
        videoStream.getTracks().forEach(track => track.stop());
        clearTimeout(timeoutId);
        clearInterval(detectionid);
    }
    video.style.display = 'none';

}

video.addEventListener('play', () => {

    document.getElementById('camload').style.display = 'none';
    const videoCanvas = document.createElement('canvas'); // Create a separate canvas for the video
    videoCanvas.width = 309;
    videoCanvas.height = 233;
    const videoCtx = videoCanvas.getContext('2d');
    const canvas = faceapi.createCanvasFromMedia(video);
    document.body.append(canvas);
    const displaySize = { width: video.width, height: video.height };
    faceapi.matchDimensions(canvas, displaySize);
    detectionid = setInterval(async () => {
        videoCtx.drawImage(video, 0, 0, videoCanvas.width, videoCanvas.height); // Draw video frame onto the separate canvas
        const detections = await faceapi.detectAllFaces(video, new faceapi.TinyFaceDetectorOptions({ scoreThreshold: 0.5 })).withFaceLandmarks().withFaceExpressions();
        const resizedDetections = faceapi.resizeResults(detections, displaySize);
        canvas.getContext('2d').clearRect(0, 0, canvas.width, canvas.height);
        if (resizedDetections.length > 0 && captured === 0) {
            if (resizedDetections.length == 1) {
                faceapi.draw.drawDetections(canvas, resizedDetections);

                const firstDetection = resizedDetections[0];

                if (firstDetection.alignedRect._box._width > 80 && firstDetection.alignedRect._box._height > 80 && firstDetection.alignedRect._score > 0.8) {
                    document.getElementById('video').style.border = '3px solid #72F71C';


                    document.getElementById('faceinstructions').style.color = 'green';
                    document.getElementById('faceinstructions').innerText = 'Look into the screen';
                    document.getElementById('faceinstructions').style.display = 'block';

                    const lastFrameBase64 = videoCanvas.toDataURL();
                    captured = 1;
                    let imgele = document.getElementById("imagedisplay")
                    imgele.src = lastFrameBase64;

                    setTimeout(function () {


                        stopVideo();
                        imgele.style.display = 'block';

                        base64Data = lastFrameBase64.substring("data:image/png;base64,".length);

                        $.ajax({
                            type: 'POST',
                            url: LoginControllerUrl + "/AuthenticatUser",
                            contentType: 'application/json',
                            dataType: 'json',
                            data: JSON.stringify({
                                authenticationScheme: "WEB_FACE",
                                authenticationData: base64Data
                            }),
                            beforeSend: function () {
                                //document.getElementById('faceinstructions').innerHTML = 'Verification is in process...';
                                //document.getElementById('faceinstructions').style.color = 'blue';
                                showLoder();

                            },
                            complete: function () {
                                document.getElementById('faceinstructions').style.display = 'none';
                                document.getElementById('faceinstructions').innerHTML = 'Please come closer!!!';
                                document.getElementById('faceinstructions').style.color = 'red';
                                hideLoder();

                            },
                            success: function (result, status, xhr) {
                                if (result.success) {
                                    show();
                                }
                                else {

                                    if (result.errorCode == 103) {
                                        swal({ type: 'info', title: "Account Suspended", text: result.message }, function (isConfirm) {
                                            if (isConfirm) {

                                                deleteCookies();
                                                location.reload();
                                                //if (redirectUrl == "")
                                                //    window.location.href = LoginControllerUrl + "/Error?error=Account Suspended&error_description=" + result.message
                                                //else
                                                //    window.location.href = redirectUrl + "?error=Account Suspended&error_description=" + result.message + "&state=" + state
                                            }
                                        });
                                    }
                                    else {
                                        swal({
                                            type: 'info', title: "Face Authentication Failed", text: result.message, showCancelButton: true, cancelButtonText: "Cancel", confirmButtonText: "Retry",
                                        }, function (isConfirm) {
                                            if (isConfirm) {
                                                document.getElementById("imagedisplay").style.display = 'none';
                                                document.getElementById('video').style.border = 'none';



                                                startVideo();
                                                captured = 0;
                                                /*document.getElementById("camblock").style.display = "block";*/
                                                /*document.getElementById('verifyface').style.display = 'block';*/
                                            }
                                            else {
                                                location.reload();
                                            }

                                        });

                                    }


                                    return false;
                                }
                            },
                            error: function (xhr, status, error) {
                                // Handle errors here
                                console.error("An error occurred: " + status + " - " + error);
                            }

                        })
                    }, 1000);




                }
                else {

                    var facei = document.getElementById('faceinstructions')
                    facei.style.display = 'block';

                    document.getElementById('video').style.border = '3px solid red';
                    document.getElementById('faceinstructions').innerHTML = "Please come closer!!!"


                }
            }
            else if (resizedDetections.length > 1) {
                document.getElementById('faceinstructions').style.color = "red";
                document.getElementById('faceinstructions').innerHTML = "Multiple Faces Detected!!!";
            }

        }
    }, 100);


})

const observer = new MutationObserver(mutationsList => {
    for (let mutation of mutationsList) {
        if (mutation.type === 'attributes' && mutation.attributeName === 'style') {
            const visibility = video.style.display;
            const textinstruction = document.getElementById('faceinstructions').innerHTML;
            if (visibility === 'none') {
                setTimeout(() => {
                    document.getElementById('faceinstructions').style.display = 'none'; // Hide the paragraph if video is hidden
                }, 500);

            }
        }
    }
});

observer.observe(video, { attributes: true });

document.getElementById('recapture').addEventListener('click', function () {
    startVideo();
    document.getElementById('camblock').style.display = 'none';
    document.getElementById('recapturediv').style.display = 'none';

});

document.getElementById("cancelface").addEventListener("click", function () {
    location.reload();
});

function AssistedAuthentication(method) {
    authenticationmethods.classList.toggle("show-modal");
    changeAssistedAuthScheme(method);
}
init();