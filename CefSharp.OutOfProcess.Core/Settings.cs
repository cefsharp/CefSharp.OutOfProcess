using System.Collections.Generic;

namespace CefSharp.OutOfProcess
{
    /// <summary>
    /// Initialization settings. Many of these and other settings can also configured using command-line switches.
    /// </summary>
    public sealed class Settings
    {
        /// <summary>
        /// The location where data for the global browser cache will be stored on disk. In this value is non-empty then it must be
        /// an absolute path that is must be either equal to or a child directory of RootCachePath (if RootCachePath is
        /// empty it will default to this value). If the value is empty then browsers will be created in "incognito mode" where
        /// in-memory caches are used for storage and no data is persisted to disk. HTML5 databases such as localStorage will only
        /// persist across sessions if a cache path is specified. Can be overridden for individual RequestContext instances via the
        /// RequestContextSettings.CachePath value.
        /// </summary>
        public string CachePath { get; set; }

        /// <summary>
        /// The root directory that all CefSettings.CachePath and RequestContextSettings.CachePath values must have in common. If this
        /// value is empty and CefSettings.CachePath is non-empty then it will default to the CefSettings.CachePath value.
        /// If this value is non-empty then it must be an absolute path.  Failure to set this value correctly may result in the sandbox
        /// blocking read/write access to the CachePath directory. NOTE: CefSharp does not implement the CHROMIUM SANDBOX. A non-empty
        /// RootCachePath can be used in conjuncation with an empty CefSettings.CachePath in instances where you would like browsers
        /// attached to the Global RequestContext (the default) created in "incognito mode" and instances created with a custom
        /// RequestContext using a disk based cache.
        /// </summary>
        public string RootCachePath { get; set; }

        /// <summary>
        /// Command line args passed to the BrowserProcess
        /// </summary>
        public ICollection<string> AdditionalCommandLineArgs { get; } = new List<string>();

        /// <summary>
        /// Adds the command line arg to the <see cref="AdditionalCommandLineArgs"/> collection.
        /// </summary>
        /// <param name="arg">command line arg</param>
        /// <returns>The current instance</returns>
        public Settings AddCommandLineArg(string arg)
        {
            AdditionalCommandLineArgs.Add(arg);

            return this;
        }

        /// <summary>
        /// Creates a new <see cref="Settings"/> instance with the specified
        /// <paramref name="cachePath"/>.
        /// </summary>
        /// <param name="cachePath">sets <see cref="CachePath"/></param>
        /// <returns>new Settings instance.</returns>
        public static Settings WithCachePath(string cachePath)
        {
            return new Settings { CachePath = cachePath };
        }
    }
}
