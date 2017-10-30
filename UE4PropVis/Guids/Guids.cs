
using System;

namespace UE4PropVis.Guids
{
    public static class Component
    {
        public static readonly Guid VisualizerComponent = new Guid("7C9480F6-7D8C-470B-9FE5-108BAD428ADC");
    }

    public static class Visualizer
    {
        public static readonly Guid UObject = new Guid("7802FE3A-0F30-4114-B701-65A33DD04133");
		public static readonly Guid PropertyList = new Guid("C52D5C78-D82C-411D-B5A0-E8B1C7C6B57A");
		// Testing
		public static readonly Guid PropertyValue = new Guid("4B57B038-C96A-4F9D-BBA9-481485F3E53E");
	}

	public static class Language
    {
        public static readonly Guid Cpp = new Guid("3A12D0B7-C26C-11D0-B442-00A0244A1DD2");
    }

    public static class Vendor
    {
        public static readonly Guid Microsoft = new Guid("994B45C4-E6E9-11D2-903F-00C04FA302A1");
    }
}
