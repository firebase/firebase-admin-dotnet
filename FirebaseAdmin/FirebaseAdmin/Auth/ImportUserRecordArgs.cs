// Copyright 2020, Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
  /// <summary>
  /// Represents a user account to be imported to Firebase Auth via the
  /// <a cref="o:FirebaseAuth.ImportUsersAsync">FirebaseAuth.ImportUsersAsync</a> API. Must contain at least a
  /// uid string.
  /// </summary>
  public class ImportUserRecordArgs
  {
    /// <summary>
    /// Gets or sets the uid of the user.
    /// </summary>
    public string Uid { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Gets or sets if the email was verified, null signifies that it was not specified.
    /// </summary>
    public bool? EmailVerified { get; set; }

    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets phone number of the user.
    /// </summary>
    public string PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the photo url.
    /// </summary>
    public string PhotoUrl { get; set; }

    /// <summary>
    /// Gets or sets the disabled value, null signifies that it was not specified.
    /// </summary>
    public bool? Disabled
    {
      get; set;
    }

    /// <summary>
    /// Gets or sets the UserMetadata.
    /// </summary>
    public UserMetadata UserMetadata { get; set; }

    /// <summary>
    /// Gets or sets the password hash.
    /// </summary>
    public byte[] PasswordHash { get; set; }

    /// <summary>
    /// Gets or sets the password salt.
    /// </summary>
    public byte[] PasswordSalt { get; set; }

    /// <summary>
    /// Gets or sets the user providers.
    /// </summary>
    public IEnumerable<UserProvider> UserProviders { get; set; }

    /// <summary>
    /// Gets or sets the custom claims.
    /// </summary>
    public IReadOnlyDictionary<string, object> CustomClaims { get; set; }

    /// <summary>
    /// Determines if a password was set.
    /// </summary>
    /// <returns>bool equivalent to if the PasswordHash is defined.</returns>
    public bool HasPassword()
    {
      return this.PasswordHash != null;
    }

    /// <summary>
    /// Verifies ImportUserRecordArgs properties by invoking UserRecordArgs validation functions and
    /// returns a dictionary containing the values to be serialized.
    /// </summary>
    /// <returns>Read-only dictionary containing all defined properties.</returns>
    public IReadOnlyDictionary<string, object> GetProperties()
    {
      Dictionary<string, object> properties = new Dictionary<string, object>();

      UserRecordArgs.CheckUid(this.Uid, true);
      properties.Add("localId", this.Uid);

      if (!string.IsNullOrEmpty(this.Email))
      {
          UserRecordArgs.CheckEmail(this.Email);
          properties.Add("email", this.Email);
      }

      if (!string.IsNullOrEmpty(this.PhotoUrl))
      {
          UserRecordArgs.CheckPhotoUrl(this.PhotoUrl);
          properties.Add("photoUrl", this.PhotoUrl);
      }

      if (!string.IsNullOrEmpty(this.PhoneNumber))
      {
          UserRecordArgs.CheckPhoneNumber(this.PhoneNumber);
          properties.Add("phoneNumber", this.PhoneNumber);
      }

      if (!string.IsNullOrEmpty(this.DisplayName))
      {
          properties.Add("displayName", this.DisplayName);
      }

      if (this.UserMetadata != null)
      {
          if (this.UserMetadata.CreationTimestamp != null)
          {
              properties.Add("createdAt", this.UserMetadata.CreationTimestamp);
          }

          if (this.UserMetadata.LastSignInTimestamp != null)
          {
              properties.Add("lastLoginAt", this.UserMetadata.LastSignInTimestamp);
          }
      }

      if (this.PasswordHash != null)
      {
          properties.Add("passwordHash", UrlSafeBase64Encode(this.PasswordHash));
      }

      if (this.PasswordSalt != null)
      {
          properties.Add("salt", UrlSafeBase64Encode(this.PasswordSalt));
      }

      if (this.UserProviders != null && this.UserProviders.Count() > 0)
      {
          properties.Add("providerUserInfo", new List<UserProvider>(this.UserProviders));
      }

      if (this.CustomClaims != null && this.CustomClaims.Count > 0)
      {
          IReadOnlyDictionary<string, object> mergedClaims = this.CustomClaims;

          // UserRecord.CheckCustomClaims(mergedClaims);
          var serialized = UserRecordArgs.CheckCustomClaims(mergedClaims);
          properties.Add(
              UserRecord.CustomAttributes,
              serialized);

          /*
          properties.Add(
            UserRecord.CustomAttributes,
            JsonConvert.SerializeObject(mergedClaims));
          */
      }

      if (this.EmailVerified != null)
      {
        properties.Add("emailVerified", this.EmailVerified);
      }

      if (this.Disabled != null)
      {
        properties.Add("disabled", this.Disabled);
      }

      return properties;
    }

    private static string UrlSafeBase64Encode(byte[] bytes)
    {
      var base64Value = Convert.ToBase64String(bytes);
      return base64Value.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
  }
}
