namespace Red.Wine.Picker
{
    public class Pick
    {
        public Pick(string name, PickConfig pickConfig = null)
        {
            Name = name;
            PickConfig = pickConfig;
        }

        public string Name { get; set; }
        public PickConfig PickConfig { get; set; }
    }
}
