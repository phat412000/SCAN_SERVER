
namespace GIAO_DIEN.backActionChild
{
    class PolyBackAction : BackAction
    {
        public int polyName { get; set; }
        public double mouseX { get; set; }
        public double mouseY { get; set; }

        public override string ToString()
        {
            return $"poly {polyName}: {mouseX} {mouseY}";
        }
    }
}
