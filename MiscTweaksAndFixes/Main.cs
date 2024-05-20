using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints.JsonSystem.Converters;
using Kingmaker.SharedTypes;

using UniRx;

namespace MiscTweaksAndFixes
{
    internal partial class Main
    {
        //[Init]
        //internal static void Init()
        //{
        //    IDisposable? subscription = null;

        //    subscription = Triggers.BlueprintsCache_Init.Take(1).Subscribe(_ =>
        //    {
        //        var assets = UnityObjectConverter.AssetList.m_Entries
        //            .GroupBy(entry => entry.FileId)
        //            .Select(group => (group.Key, group.GroupBy(entry => entry.Asset.GetType())));



        //        subscription?.Dispose();
        //    });
        //}
    }
}
