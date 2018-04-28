﻿namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
    using System.Collections.Generic;
    using System.Linq;

    using Clipboard.Interfaces;

    using Data;
    using Data.Factories.Interfaces;
    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;
    using Exceptions;

    class ClipboardDataControlPackageFactory : IClipboardDataControlPackageFactory
    {
        readonly IClipboardDataPackageFactory dataPackageFactory;
		readonly IMainThreadInvoker mainThreadInvoker;

		readonly IEnumerable<IClipboardDataControlFactory> controlFactories;

        public ClipboardDataControlPackageFactory(
            IClipboardDataPackageFactory dataPackageFactory,
            IEnumerable<IClipboardDataControlFactory> controlFactories,
            IMainThreadInvoker mainThreadInvoker)
        {
            this.dataPackageFactory = dataPackageFactory;
            this.controlFactories = controlFactories.OrderBy(x => x.Priority);
            this.mainThreadInvoker = mainThreadInvoker;
        }

        public IClipboardDataControlPackage CreateFromCurrentClipboardData()
        {
            try
            {
                var dataPackage = dataPackageFactory.CreateFromCurrentClipboardData();
                return CreateFromDataPackage(dataPackage);
            }
            catch (ClipboardFormatNotUnderstoodException)
            {
                //TODO: #20 - adding support for custom data
                return null;
            }
        }

        ClipboardDataControlPackage CreateDataControlPackageFromDataPackage(IClipboardDataPackage dataPackage)
        {
            var control = CreateControlFromDataPackage(dataPackage);
            if (control == null)
            {
                return null;
            }

            var package = new ClipboardDataControlPackage(dataPackage, control);
            return package;
        }

        public IClipboardDataControlPackage CreateFromDataPackage(IClipboardDataPackage dataPackage)
        {
            ClipboardDataControlPackage package = null;
            mainThreadInvoker.Invoke(
                () => package = CreateDataControlPackageFromDataPackage(dataPackage));

            return package;
        }

        IClipboardControl CreateControlFromDataPackage(IClipboardDataPackage dataPackage)
		{
			var matchingFactory = controlFactories.FirstOrDefault(x => x.CanBuildControl(dataPackage));
			if (matchingFactory != null)
			{
				return matchingFactory.BuildControl(dataPackage);
			}

			return null;
        }
    }
}