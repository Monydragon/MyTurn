using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyTurn.Presentation;

namespace MyTurn.Desktop;

internal sealed class JrpgWindowRenderer
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly BitmapTextRenderer _text;

    public JrpgWindowRenderer(SpriteBatch spriteBatch, Texture2D pixel, BitmapTextRenderer text)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        _text = text;
    }

    public void DrawWindow(Rectangle bounds, string? title = null)
    {
        Fill(bounds, new Color(12, 18, 32, 236));
        Stroke(bounds, new Color(232, 226, 202));
        Stroke(DesktopLayout.Inset(bounds, 4), new Color(62, 88, 126));

        if (!string.IsNullOrWhiteSpace(title))
        {
            Fill(new Rectangle(bounds.X + 8, bounds.Y + 8, Math.Min(bounds.Width - 16, 220), 26), new Color(26, 38, 64));
            DrawText(title, bounds.X + 18, bounds.Y + 15, Color.Gold, 2);
        }
    }

    public void DrawMenu(IReadOnlyList<MenuOptionViewModel> options, Rectangle bounds, int scale = 3)
    {
        var y = bounds.Y;
        var lineHeight = scale * 14;
        var maxRows = Math.Max(1, bounds.Height / lineHeight);

        foreach (var option in options.Take(maxRows))
        {
            var marker = option.IsSelected ? ">" : " ";
            var color = option.IsSelected ? Color.Gold : Color.White;
            DrawText($"{marker} {option.Label}", bounds.X, y, color, scale, bounds.Width);

            if (!string.IsNullOrWhiteSpace(option.Detail) && y + lineHeight < bounds.Bottom)
            {
                DrawText(option.Detail, bounds.X + (scale * 12), y + (scale * 9), Color.LightGray, Math.Max(1, scale - 1), bounds.Width - 24);
                y += lineHeight / 2;
            }

            y += lineHeight;
        }
    }

    public void DrawWrappedText(string text, Rectangle bounds, Color color, int scale = 2)
    {
        var maxChars = Math.Max(1, bounds.Width / Math.Max(1, scale * 6));
        var lineHeight = scale * 10;
        var y = bounds.Y;
        var line = string.Empty;

        foreach (var word in Sanitize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length + word.Length + 1 > maxChars)
            {
                DrawText(line, bounds.X, y, color, scale, bounds.Width);
                y += lineHeight;
                line = word;

                if (y + lineHeight > bounds.Bottom)
                {
                    return;
                }
            }
            else
            {
                line = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
            }
        }

        if (!string.IsNullOrEmpty(line) && y + lineHeight <= bounds.Bottom)
        {
            DrawText(line, bounds.X, y, color, scale, bounds.Width);
        }
    }

    public void DrawHpBar(Rectangle bounds, int current, int max)
    {
        var ratio = Math.Clamp((float)Math.Max(0, current) / Math.Max(1, max), 0f, 1f);
        var fillColor = ratio < 0.3f ? Color.OrangeRed : ratio < 0.6f ? Color.Gold : new Color(56, 188, 96);

        Fill(bounds, new Color(42, 46, 58));
        Fill(new Rectangle(bounds.X, bounds.Y, (int)(bounds.Width * ratio), bounds.Height), fillColor);
        Stroke(bounds, new Color(232, 226, 202));
    }

    public void DrawText(string text, int x, int y, Color color, int scale = 2, int? maxWidth = null)
    {
        var sanitized = Sanitize(text);

        if (maxWidth is not null)
        {
            var maxChars = Math.Max(1, maxWidth.Value / Math.Max(1, scale * 6));
            sanitized = sanitized.Length > maxChars ? sanitized[..maxChars] : sanitized;
        }

        _text.Draw(sanitized, new Vector2(x, y), color, scale);
    }

    public void Fill(Rectangle rectangle, Color color)
    {
        _spriteBatch.Draw(_pixel, rectangle, color);
    }

    public void Stroke(Rectangle rectangle, Color color)
    {
        Fill(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 2), color);
        Fill(new Rectangle(rectangle.X, rectangle.Bottom - 2, rectangle.Width, 2), color);
        Fill(new Rectangle(rectangle.X, rectangle.Y, 2, rectangle.Height), color);
        Fill(new Rectangle(rectangle.Right - 2, rectangle.Y, 2, rectangle.Height), color);
    }

    private static string Sanitize(string text)
    {
        return text.Replace("[", string.Empty).Replace("]", string.Empty).ToUpperInvariant();
    }
}
