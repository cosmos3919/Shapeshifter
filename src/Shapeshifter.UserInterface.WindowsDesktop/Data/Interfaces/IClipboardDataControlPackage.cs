﻿using System.Collections.Generic;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    public interface IClipboardDataControlPackage : IClipboardDataPackage
    {
        IClipboardControl Control { get; }
    }
}
