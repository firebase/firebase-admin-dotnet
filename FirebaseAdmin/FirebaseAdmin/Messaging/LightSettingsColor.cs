using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// A class representing color in LightSettings.
    /// </summary>
    public class LightSettingsColor
    {
        /// <summary>
        /// Gets or sets the red component.
        /// </summary>
        public float Red { get; set; }

        /// <summary>
        /// Gets or sets the green component.
        /// </summary>
        public float Green { get; set; }

        /// <summary>
        /// Gets or sets the blue component.
        /// </summary>
        public float Blue { get; set; }

        /// <summary>
        /// Gets or sets the alpha component.
        /// </summary>
        public float Alpha { get; set; }
    }
}
