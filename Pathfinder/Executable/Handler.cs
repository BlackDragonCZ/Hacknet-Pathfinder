using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder.Util;

namespace Pathfinder.Executable
{
    public static class Handler
    {
        internal static readonly Dictionary<string, Tuple<IInterface, string>> ModExecutables =
            new Dictionary<string, Tuple<IInterface, string>>();

        /// <summary>
        /// Adds an executable interface by id.
        /// </summary>
        /// <returns>The full mod id if added to the game, <c>null</c> otherwise.</returns>
        /// <param name="id">The Executable Identifier to try and add.</param>
        /// <param name="inter">The interface object.</param>
        public static string RegisterExecutable(string id, IInterface inter)
        {
            if (Pathfinder.CurrentMod == null && !Extension.Handler.CanRegister)
                throw new InvalidOperationException("RegisterExecutable can not be called outside of mod or extension loading.");
            id = Pathfinder.CurrentMod != null ? Utility.GetId(id, throwFindingPeriod: true) : Extension.Handler.ActiveInfo.Id+"."+id;
            Logger.Verbose("{0} {1} is attempting to add executable interface {2} with id {3}",
                           Pathfinder.CurrentMod != null ? "Mod" : "Extension",
                           Pathfinder.CurrentMod?.GetCleanId() ?? Extension.Handler.ActiveInfo.Id,
                           inter.GetType().FullName,
                           id);
            if (ModExecutables.ContainsKey(id))
                return null;
            var type = inter.GetType();
            var fileData = id + "\nldloc.args\ncall Pathfinder.Executable.Instance [" + type.Assembly.GetName().Name + ".dll]"
                                                     + type.FullName + "=" + id + "()";
            if (fileData.Length < 1 || ModExecutables.Any(pair => pair.Value.Item2 == fileData))
                throw new ArgumentException("created data for '" + id + "' is not unique");
            ModExecutables.Add(id, new Tuple<IInterface, string>(inter, fileData));
            return id;
        }

        /// <summary>
        /// Removes an executable by it's id
        /// </summary>
        /// <param name="id">The full id of the executable</param>
        /// <returns>If successful</returns>
        public static bool UnregisterExecutable(string id)
        {
            id = Utility.GetId(id);
            return !ModExecutables.ContainsKey(id) || ModExecutables.Remove(id);
        }

        /// <summary>
        /// Determines whether the file data is for a mod interface.
        /// </summary>
        /// <returns><c>true</c>, if the file data is for a mod interface, <c>false</c> otherwise.</returns>
        /// <param name="fileData">File data.</param>
        public static bool IsFileDataForModExe(string fileData)
        {
            var dataLines = fileData.Split('\n');
            return dataLines.Length >= 3 && dataLines[1] == "ldloc.args"
                            && dataLines[2].StartsWith("call Pathfinder.Executable.Instance", StringComparison.Ordinal)
                            && dataLines[2].EndsWith("=" + dataLines[0] + "()", StringComparison.Ordinal)
                            && ModExecutables.Any(pair => pair.Value.Item2 == dataLines[0]);
        }

        /// <summary>
        /// Gets the standard file data by id.
        /// </summary>
        /// <returns>The standard file data or <c>null</c> if it doesn't exist.</returns>
        /// <param name="id">Executable Identifier.</param>
        /// <param name="requiresModId">If set to <c>true</c> id requires a prefixing mod identifier delimated by period.</param>
        public static string GetStandardFileDataBy(string id, bool requiresModId = false)
        {
            if (requiresModId && id.IndexOf('.') == -1)
                throw new ArgumentException("must contain a mod id and delimter (.)", nameof(id));
            id = Utility.GetId(id, requiresModId, true);
            return ModExecutables.TryGetValue(id, out var result) ? result.Item2 : null;
        }

        /// <summary>
        /// Gets the first standard file data by interface.
        /// </summary>
        /// <returns>The standard file data or <c>null</c> if it doesn't exist.</returns>
        /// <param name="inter">The Executable Interface</param>
        public static string GetStandardFileDataBy(IInterface inter)
        {
            foreach (var pair in ModExecutables)
                if (pair.Value.Item1 == inter)
                    return GetStandardFileDataBy(pair.Key);
            return null;
        }
    }
}
