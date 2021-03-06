﻿using Riff.Data;
using System.IO;
using System.Threading.Tasks;

namespace Riff.Authentication
{
    public interface IProfileCache
    {
        Task<UserProfile> GetProfileAsync();
        Task SetProfileAsync(UserProfile profile);
        Task<Stream> GetPhotoAsync();
        Task SetPhotoAsync(Stream stream);
    }
}
