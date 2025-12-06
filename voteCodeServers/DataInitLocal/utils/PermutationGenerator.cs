using System.Security.Cryptography;

public class PermutationGenerator
{
    private readonly int _n;
    private readonly List<int> _indices;

    public PermutationGenerator(int n)
    {
        _n = n;
        _indices = Enumerable.Range(0, n).ToList();
        Shuffle(_indices);
    }

    public List<int> Generate()
    {
        return _indices.Select(x => x + 1).ToList();
    }

    public int GetValue(int index)
    {
        if (index < 0 || index >= _n)
            throw new ArgumentOutOfRangeException(nameof(index));

        return _indices[index] + 1;
    }

    private void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}