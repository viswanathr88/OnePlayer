﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Riff.Data
{
    public interface IDriveItemAccessor : IDriveItemReadOnlyAccessor
    {
        DriveItem Add(DriveItem item);

        DriveItem Update(DriveItem item);

        void Delete(string id);
    }
}
