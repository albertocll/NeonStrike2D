using NeonStrike2D.Backend.Api.Security;

namespace NeonStrike2D.Backend.Tests;

public class MatchResultRulesTests
{
    [Fact]
    public void Tope_OleadaUno()
    {
        // 150 * 3 enemigos + 50 de bonus + 2000 por tiempo = 2500, por 2.
        Assert.Equal(5000, MatchResultRules.MaxScoreFor(1));
        Assert.Equal(5000, MatchResultRules.MaxScoreFor(0));
    }

    [Fact]
    public void Tope_OleadaDiez_IncluyeUnJefe()
    {
        // Enemigos 150 * (3+...+12) = 11 250; bonus 50 * 55 = 2750; tiempo 20 000; jefe 3000.
        Assert.Equal(2 * (11250 + 2750 + 20000 + 3000), MatchResultRules.MaxScoreFor(10));
    }

    [Fact]
    public void Tope_CreceConLaOleadaYNoSeDesborda()
    {
        Assert.True(MatchResultRules.MaxScoreFor(60) > MatchResultRules.MaxScoreFor(59));
        Assert.True(MatchResultRules.MaxScoreFor(MatchResultRules.MaxWave) < int.MaxValue);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(12, 9876, true)]
    [InlineData(1, 5000, true)]
    [InlineData(1, 5001, false)]
    [InlineData(-1, 0, false)]
    [InlineData(5, -1, false)]
    [InlineData(1001, 0, false)]
    public void IsValid(int wave, int score, bool expected)
    {
        Assert.Equal(expected, MatchResultRules.IsValid(wave, score));
    }
}
