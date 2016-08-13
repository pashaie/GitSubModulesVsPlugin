﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GitSubmodules.Enumerations;
using GitSubmodules.Helper;

namespace GitSubmodules.Mvvm.Model
{
    /// <summary>
    /// Model that contains all data of a git submodule
    /// </summary>
    public sealed class Submodule : ModelBase
    {
        #region Public Properties

        /// <summary>
        /// The name of the submodule
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The id of the submodule (SHA1)
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The commit id of the submodule (SHA1)
        /// </summary>
        public string CommitId { get; private set; }

        /// <summary>
        /// The background color for this module that indicate the crrent status of it
        /// </summary>
        public SolidColorBrush BackgroundColor { get; private set; }

        /// <summary>
        /// The current status of this module
        /// </summary>
        public SubModuleStatus Status { get; private set; }

        /// <summary>
        /// The current status text of this module
        /// </summary>
        public string StatusText { get; private set; }

        /// <summary>
        /// The <see cref="Image"/> for the health status of this <see cref="Submodule"/>
        /// </summary>
        public BitmapSource HealthImage
        {
            get { return _healthImage; }
            private set
            {
                _healthImage = value;
                OnPropertyChanged();
            }
        }

        #endregion Public Properties

        #region Private Fields

        /// <summary>
        /// The Backing-field for <see cref="HealthImage"/>
        /// </summary>
        private BitmapSource _healthImage;

        #endregion Private Fields

        #region Internal Constructor

        /// <summary>
        /// Creates a new <see cref="Submodule"/> with the given information
        /// </summary>
        /// <param name="solutionPath">The path to the current opend solution</param>
        /// <param name="subModuleInformation">The <see cref="string"/>
        /// that contains informations of the submodule</param>
        internal Submodule(string solutionPath, string subModuleInformation)
        {
            if(string.IsNullOrEmpty(subModuleInformation))
            {
                return;
            }

            var lineSplit = subModuleInformation.TrimStart().Split(' ');

            Id       = lineSplit.FirstOrDefault();
            Name     = lineSplit.ElementAtOrDefault(1) ?? "???";
            CommitId = lineSplit.ElementAtOrDefault(2) ?? "???";

            Id = !string.IsNullOrEmpty(Id)
                    ? Id.Replace("U", string.Empty)
                        .Replace("+",string.Empty)
                        .Replace("-",string.Empty)
                        .TrimStart()
                    : "???";

            if(!string.IsNullOrEmpty(CommitId))
            {
                CommitId = CommitId.TrimStart('(').TrimEnd(')');
            }

            SetSubModuleStatus(solutionPath, subModuleInformation);

            SetBackgroundColor();

            UpdateHealthStatus(HealthStatus.Okay);
        }

        #endregion Internal Constructor

        #region Internal Methods

        /// <summary>
        /// Set the background color for thsi module based on the <see cref="Status"/>
        /// </summary>
        internal void SetBackgroundColor()
        {
            switch(Status)
            {
                case SubModuleStatus.Unknown:
                    BackgroundColor = Brushes.LightGray;
                    break;

                case SubModuleStatus.NotInitialized:
                    BackgroundColor = Brushes.LightCoral;
                    break;

                case SubModuleStatus.Initialized:
                    BackgroundColor = Brushes.Yellow;
                    break;

                case SubModuleStatus.MergeConflict:
                    BackgroundColor = Brushes.DarkOrange;
                    break;

                case SubModuleStatus.Current:
                    BackgroundColor = Brushes.YellowGreen;
                    break;

                case SubModuleStatus.NotCurrent:
                    BackgroundColor = Brushes.LightSkyBlue;
                    break;

                default:
                    BackgroundColor = Brushes.LightGray;
                    break;
            }
        }

        /// <summary>
        /// Set the <see cref="Status"/> of this module, based on the given information
        /// </summary>
        /// <param name="solutionPath">The path to the current opend solution</param>
        /// <param name="subModuleInformation">The <see cref="string"/>
        /// that contains informations of the submodule</param>
        internal void SetSubModuleStatus(string solutionPath, string subModuleInformation)
        {
            if(string.IsNullOrEmpty(subModuleInformation))
            {
                return;
            }

            switch(subModuleInformation.FirstOrDefault())
            {
                case ' ':
                    Status     = SubModuleStatus.Current;
                    StatusText = "Submodule is current";
                    break;

                case 'U':
                    Status     = SubModuleStatus.MergeConflict;
                    StatusText = "Submodule has merge conflicts";
                    break;

                case '+':
                    Status     = SubModuleStatus.NotCurrent;
                    StatusText = "Submodule is not current";
                    break;

                case '-':
                    SetModuleRegistrationStatus(solutionPath);
                    break;

                default:
                    Status     = SubModuleStatus.Unknown;
                    StatusText = "Submodule status is unknown";
                    break;
            }
        }

        /// <summary>
        /// Set the registration status of a submodule (loking into the /.git/config)
        /// </summary>
        /// <param name="solutionPath">The path to the current opend solution</param>
        internal void SetModuleRegistrationStatus(string solutionPath)
        {
            if(string.IsNullOrEmpty(solutionPath))
            {
                return;
            }

            var gitConfigFilePath = Path.Combine(solutionPath, ".git", "config");

            if(!File.Exists(gitConfigFilePath))
            {
                Status     = SubModuleStatus.Unknown;
                StatusText = "Git config file not found";
                return;
            }

            try
            {
                using(var streamReader = new StreamReader(File.Open(gitConfigFilePath, FileMode.Open, FileAccess.Read)))
                {
                    if(streamReader.ReadToEnd().Contains("[submodule \"" + Name + "\"]"))
                    {
                        Status     = SubModuleStatus.Initialized;
                        StatusText = "Submodule is initialized";
                        return;
                    }

                    Status     = SubModuleStatus.NotInitialized;
                    StatusText = "Submodule is not initialized";
                }
            }
            catch(Exception exception)
            {
                Status     = SubModuleStatus.Unknown;
                StatusText = exception.Message;
            }
        }

        internal void UpdateHealthStatus(HealthStatus healthStatus)
        {
            string healthImageFile;

            switch(healthStatus)
            {
                case HealthStatus.Unknown:
                    healthImageFile = "Unknown.png";
                    break;

                case HealthStatus.Okay:
                    healthImageFile = "Okay.png";
                    break;

                case HealthStatus.Warning:
                    healthImageFile = "Warning.png";
                    break;

                case HealthStatus.Error:
                    healthImageFile = "Error.png";
                    break;

                default:
                    healthImageFile = "Unknown.png";
                    break;
            }

            var resourceName = Assembly.GetExecutingAssembly()
                                       .GetManifestResourceNames()
                                       .FirstOrDefault(found => found.Contains(healthImageFile));

            if(string.IsNullOrEmpty(resourceName))
            {
                return;
            }

            try
            {
                using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if(stream == null)
                    {
                        return;
                    }

                    HealthImage = BitmapFrame.Create(stream);
                }
            }
            catch(Exception exception)
            {
                Debug.WriteLine(exception.ToString());
            }
        }

        #endregion Internal Methods
    }
}
