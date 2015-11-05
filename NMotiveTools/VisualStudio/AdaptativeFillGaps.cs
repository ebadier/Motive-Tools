using NMotive;

namespace NMotiveTools
{
    public class AdaptativeFillGaps : NMotive.FillGaps
    {
        public override Result Process(Take take)
        {
            MaxGapFillWidth = (int)(take.FrameRate * 0.05);
            return base.Process(take);
        }
    }
}