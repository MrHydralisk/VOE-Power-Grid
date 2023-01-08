using UnityEngine;
using Verse;

namespace VOEPowerGrid
{
    [StaticConstructorOnStartup]
    public static class TexOutposts
    {
        public static readonly Texture2D ConnectTex = ContentFinder<Texture2D>.Get("Icons/Connect");
    }
}
