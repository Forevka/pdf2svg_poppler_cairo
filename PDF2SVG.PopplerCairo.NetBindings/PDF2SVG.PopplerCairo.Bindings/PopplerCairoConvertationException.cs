namespace PDF2SVG.PopplerCairo.Bindings;

public class PopplerCairoConvertationException : InvalidOperationException
{
    public PopplerCairoConvertationException(string message, Exception ex) : base(message, ex)
    {
    }

    public PopplerCairoConvertationException(string message) : base(message)
    {
    }
}
