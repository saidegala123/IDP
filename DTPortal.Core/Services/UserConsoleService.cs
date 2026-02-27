using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DTPortal.Core.Domain.Models;
using DTPortal.Core.Domain.Lookups;
using DTPortal.Core.Domain.Services;
using DTPortal.Core.Domain.Repositories;
using DTPortal.Core.Domain.Services.Communication;
using DTPortal.Common;
using Microsoft.Extensions.Logging;
using static DTPortal.Common.CommonResponse;
using System.Text.RegularExpressions;
using DTPortal.Core.Constants;
using DTPortal.Core.Utilities;

namespace DTPortal.Core.Services
{
    public class UserConsoleService : IUserConsoleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserConsoleService> _logger;

        public UserConsoleService(IUnitOfWork unitOfWork, ILogger<UserConsoleService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
/*
        public async Task<bool> CheckPasswordComplexity(string password, PasswordPolicy passwordPolicy)
        {
            if (password.Length < passwordPolicy.MinimumPwdLength
                || password.Length > passwordPolicy.MaximumPwdLength)
            {
                return false;
            }

            switch(passwordPolicy.PwdContains)
            {
                case 1:
                    {
                        return Regex.IsMatch(password, @"^[a-zA-Z]+$");
                        //break;
                    }
                case 2:
                    {
                        return Regex.IsMatch(password, @"^[0-9]+$");
                        //break;
                    }
                case 3:
                    {
                        return Regex.IsMatch(password, @"^[a-zA-Z0-9]+$");
                        //break;
                    }
                case 4:
                    {
                        return Regex.IsMatch(password, @"^(?=.*\d)(?=.*[a-zA-Z])(?!.*\s)[0-9a-zA-Z]*$");
                        //break;
                    }
                case 5:
                    {
                        return Regex.IsMatch(password, @"^(?=.*[0-9])(?=.*[!@#$%^_&*])(?=.*[a-z])(?=.*[A-Z])[a-zA-Z0-9!@#$%^_&*]{1,106}$");
                        //break;
                    }

                default:
                    {
                        return true;
                    }

            }

            //return true;
        }
*/
        public async Task<Response> ChangePassword(int userId, string oldPassword, string newPassword)
        {

            // Variable declaration
            var id = 24;
            var response = new Response();

            var userInDb = await _unitOfWork.Users.GetByIdAsync(userId);
            if(null == userInDb)
            {
                response.Success = false;
                response.Message = "No user found with ID";
                return response;
            }

            if(!userInDb.Status.Equals("NEW") && !userInDb.Status.Equals("ACTIVE"))
            {
                response.Success = false;
                response.Message = "User status is not ACTIVE/NEW";
                return response;
            }

            var passwordPolicy = await _unitOfWork.PasswordPolicy.GetByIdAsync(1);
            if (passwordPolicy == null)
            {
                response.Success = false;
                response.Message = "Internal server error";
                return response;
            }

            var isAccept = PasswordValidation.CheckPasswordComplexity(newPassword,
                passwordPolicy);
            if (false == isAccept)
            {
                response.Success = false;
                response.Message = String.Format(DTInternalConstants.PasswordPolicyMismatch,
                    passwordPolicy.MinimumPwdLength, passwordPolicy.MaximumPwdLength);
                return response;
            }

            // Get Encryption Key
            var EncKey = await _unitOfWork.EncDecKeys.GetByIdAsync(id);
            if (null == EncKey)
            {
                response.Success = false;
                response.Message = "Internal error occurred";
                return response;
            }

            string encryptionPassword = Encoding.UTF8.GetString(EncKey.Key1);
            var DecryptedPasswd = string.Empty;

            if (!string.IsNullOrEmpty(userInDb.AuthData))
            {
                var passwordsInDb = userInDb.AuthData.Split(',');
                if (passwordsInDb.Count() > 0)
                {
                    if (passwordPolicy.PasswordHistory > 0)
                    {
                        for (int i = 0; i < passwordPolicy.PasswordHistory && i < passwordsInDb.Count(); i++)
                        {
                            if (!string.IsNullOrEmpty(passwordsInDb[i]))
                            {
                                try
                                {
                                    // Decrypt Password
                                    DecryptedPasswd = EncryptionLibrary.DecryptText(passwordsInDb[i],
                                        encryptionPassword, "appshield3.0");
                                }
                                catch (Exception error)
                                {
                                    _logger.LogError("DecryptText failed, found exception: {0}",
                                        error.Message);
                                    // Log the exception 
                                    response.Success = false;
                                    response.Message = "DecryptText failed, found exception";
                                    return response;
                                }

                                if (DecryptedPasswd.Equals(newPassword))
                                {
                                    response.Success = false;
                                    response.Message = String.Format("New password matches one of the last {0} passwords",
                                        passwordPolicy.PasswordHistory);
                                    return response;
                                }
                            }
                        }

                    }
                }
            }

            var oldPasswordinDb = await _unitOfWork.UserAuthData.GetUserAuthDataAsync
                (userInDb.Uuid, "PASSWORD");

            try
            {
                // Decrypt Password
                DecryptedPasswd = EncryptionLibrary.DecryptText(oldPasswordinDb.AuthData,
                    encryptionPassword, "appshield3.0");
            }
            catch (Exception error)
            {
                _logger.LogError("DecryptText failed, found exception: {0}",
                    error.Message);
                // Log the exception 
                response.Success = false;
                response.Message = "DecryptText failed, found exception";
                return response;
            }


            if (DecryptedPasswd != oldPassword)
            {
                response.Success = false;
                response.Message = "Old password is not matched";
                return response;
            }

            if (DecryptedPasswd == newPassword)
            {
                response.Success = false;
                response.Message = "New password and Old password is same";
                return response;
            }

            // Encrypt Password
            var EncryptedPasswd = EncryptionLibrary.EncryptText(newPassword,
                encryptionPassword, "appshield3.0");

            var UserpasswordDetail = await _unitOfWork.UserLoginDetail.GetUserLoginDetailAsync(userId.ToString());
            if (null == UserpasswordDetail)
            {
                // User Login Details
                var userPasswordDetail = new UserLoginDetail
                {
                    UserId = userInDb.Id.ToString(),
                    IsReversibleEncryption = false,
                    WrongPinCount = 0,
                    WrongCodeCount = 0,
                    DeniedCount = 0,
                    IsScrambled = false,
                    PriAuthSchId = 64
                };

                try
                {
                    await _unitOfWork.UserLoginDetail.AddAsync(userPasswordDetail);
                    await _unitOfWork.SaveAsync();
                }
                catch
                {
                    response.Success = false;
                    response.Message = "Internal server error";
                    return response;
                }
            }
            else
            {
                UserpasswordDetail.LastAuthData = EncryptedPasswd;
                UserpasswordDetail.IsReversibleEncryption = false;

                // Add entry in db
                _unitOfWork.UserLoginDetail.Update(UserpasswordDetail);

                try
                {
                    await _unitOfWork.SaveAsync();
                }
                catch
                {
                    response.Success = false;
                    response.Message = "Internal error occurred";
                    return response;
                }
            }

            var userAuthData = await _unitOfWork.UserAuthData.GetUserAuthDataAsync(userInDb.Uuid, "PASSWORD");

            userAuthData.AuthData = EncryptedPasswd;
            userAuthData.ModifiedDate = DateTime.Now;

            // Add entry in db
            _unitOfWork.UserAuthData.Update(userAuthData);

            try
            {
                await _unitOfWork.SaveAsync();
                //return true;
            }
            catch
            {
                response.Success = false;
                response.Message = "Internal error occurred";
                return response;
            }

            if (string.IsNullOrEmpty(userInDb.AuthData))
            {
                userInDb.AuthData = EncryptedPasswd;
            }
            else
            {
                var password = userInDb.AuthData.Split(',').ToList();
                if (password.Count() == passwordPolicy.PasswordHistory)
                {
                    password.Remove(password[0]);
                    userInDb.AuthData = string.Join(',', password);
                }
                userInDb.AuthData = string.Format("{0},{1}", userInDb.AuthData,
                    EncryptedPasswd);
            }
            if (userInDb.Status.Equals("NEW"))
            {
                userInDb.Status = "CHANGE_PASSWORD";
            }



            try
            {
                _unitOfWork.Users.Update(userInDb);
                await _unitOfWork.SaveAsync();

                response.Success = true;
                return response;
            }
            catch
            {
                response.Success = false;
                response.Message = "Internal error occurred";
                return response;
            }
        }

        public async Task<UserResponse> UpdateProfile(UserTable user)
        {
            var userInDb = await _unitOfWork.Users.GetByIdAsync(user.Id);
            if (userInDb.MailId != user.MailId)
            {
                // Check if user exists with the provided details
                if (await _unitOfWork.Users.IsUserExistsWithEmail(user))
                {
                    return new UserResponse("User emailid already exists");
                }
            }
            if (userInDb.MobileNo != user.MobileNo)
            {
                // Check if user exists with the provided details
                if (await _unitOfWork.Users.IsUserExistsWitMobile(user))
                {
                    return new UserResponse("User phone number already exists");
                }
            }

            if (user.AuthScheme.Equals("FIDO2"))
            {
                var isExists = await _unitOfWork.UserAuthData.GetUserAuthDataAsync(user.Uuid,"FIDO2");
                if (null == isExists)
                {
                    return new UserResponse("User FIDO2 device not registered");
                }
            }

            if(null == user.FullName)
            {
                return new UserResponse("FullName cannot be empty");
            }
            userInDb.UpdatedBy = user.UpdatedBy;
            userInDb.ModifiedDate = DateTime.Now;
            userInDb.Gender = user.Gender;
            userInDb.MailId = user.MailId;
            userInDb.MobileNo = user.MobileNo;
            userInDb.Dob = user.Dob;
            userInDb.RoleId = user.RoleId;
            userInDb.FullName = user.FullName;

            try
            {
                _unitOfWork.Users.Update(userInDb);

                await _unitOfWork.SaveAsync();


                return new UserResponse(user, "User updated successfully");
            }
            catch
            {
                // Log the exception 
                return new UserResponse("An error occurred while updating the user." +
                    " Please contact the admin.");
            }

        }

        public async Task<bool> IsUserProvisioned(UserAuthDatum userAuthData)
        {
            var isExists = await _unitOfWork.UserAuthData.IsUserAuthDataExists(userAuthData.UserId, "CHANGE_PASSWORD");
            if(false == isExists)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public async Task<UserAuthDataResponse> ProvisionUser(UserAuthDatum userAuthData)
        {

            var isExists = await _unitOfWork.UserAuthData.IsUserAuthDataExists(userAuthData.UserId, "CHANGE_PASSWORD");
            if (false == isExists)
            {
                userAuthData.CreatedDate = DateTime.Now;
                userAuthData.ModifiedDate = DateTime.Now;

                await _unitOfWork.UserAuthData.AddAsync(userAuthData);
            }
            else
            {
                var userAuthDatainDb = await _unitOfWork.UserAuthData.GetUserAuthDataAsync(userAuthData.UserId, "CHANGE_PASSWORD");
                if (null ==userAuthDatainDb)
                {
                    return new UserAuthDataResponse("Provision user failed, Please contact admin");
                }

                userAuthDatainDb.AuthData = userAuthData.AuthData;
                userAuthDatainDb.ModifiedDate = DateTime.Now;
                userAuthDatainDb.FailedLoginAttempts = userAuthData.FailedLoginAttempts;

                _unitOfWork.UserAuthData.Update(userAuthData);
            }
            try
            {
                await _unitOfWork.SaveAsync();
                return new UserAuthDataResponse(userAuthData);
            }
            catch
            {
                return new UserAuthDataResponse("Provision user failed, Please contact admin");
            }
        }


        public async Task<UserTable> GetUserAsync(int id)
        {
            return await _unitOfWork.Users.GetUserByIdWithRoleAsync(id);
        }

        public async Task<UserAuthDataResponse> GetUserAuthDataAsync(UserAuthDatum userAuthData)
        {
            var data = await _unitOfWork.UserAuthData.GetUserAuthDataAsync(userAuthData.UserId, userAuthData.AuthScheme);
            if (data == null)
            {
                return new UserAuthDataResponse("User Authdata Not Found");
            }
            else
            {
                userAuthData.AuthData = data.AuthData;
                return new UserAuthDataResponse(userAuthData);
            }
        }

        public async Task<UserAuthDataResponse> ProvisionExternalUser(UserAuthDatum userAuthData)
        {

            var isExists = await _unitOfWork.UserAuthData.IsUserAuthDataExists(userAuthData.UserId, userAuthData.AuthScheme);
            if (false == isExists)
            {
                userAuthData.CreatedDate = DateTime.Now;
                userAuthData.ModifiedDate = DateTime.Now;
                userAuthData.CreatedBy = "sysadmin";
                userAuthData.UpdatedBy = "sysadmin";
                userAuthData.FailedLoginAttempts = 0;
                userAuthData.Status = "ACTIVE";
                userAuthData.Istemporary = false;
                await _unitOfWork.UserAuthData.AddAsync(userAuthData);
            }
            else
            {
                var userAuthDatainDb = await _unitOfWork.UserAuthData.GetUserAuthDataAsync(userAuthData.UserId, userAuthData.AuthScheme);
                if (null == userAuthDatainDb)
                {
                    return new UserAuthDataResponse("Provision user failed, Please contact admin");
                }

                userAuthDatainDb.AuthData = userAuthData.AuthData;
                userAuthDatainDb.ModifiedDate = DateTime.Now;
                userAuthDatainDb.FailedLoginAttempts = userAuthData.FailedLoginAttempts;

                _unitOfWork.UserAuthData.Update(userAuthDatainDb);
            }

            try
            {
                await _unitOfWork.SaveAsync();
            }
            catch (Exception e)
            {
                return new UserAuthDataResponse(e.Message);
                // return new UserAuthDataResponse("Provision user failed, Please contact admin");
            }

            return new UserAuthDataResponse(userAuthData);

        }
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    }
}
