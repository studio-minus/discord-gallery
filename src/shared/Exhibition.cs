namespace gallery.shared;

public class Exhibition
{
    public ImageSubmission[] Images;
    public CompositionSubmission? Composition;

    public Exhibition(ImageSubmission[] images, CompositionSubmission? composition)
    {
        Images = images;
        Composition = composition;
    }
}