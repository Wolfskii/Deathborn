namespace Deathborn.Client;

/// <summary>Rolling frames-per-second estimate updated once per second.</summary>
public sealed class FpsCounter
{
    private int _frames;
    private float _accum;
    private int _fps;

    public int Fps => _fps;

    public void Update(float dt)
    {
        _frames++;
        _accum += dt;
        if (_accum < 1f) return;
        _fps = _frames;
        _frames = 0;
        _accum = 0f;
    }
}
