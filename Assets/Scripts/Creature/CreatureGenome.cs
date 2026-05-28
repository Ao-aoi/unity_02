using UnityEngine;

// 🧬 遺伝子（ゲノム）クラス：脳の設計図をただの数値配列として持つ
[System.Serializable]
public class CreatureGenome
{
    [Tooltip("何世代目か")]
    public int generation = 1;
    [Tooltip("身体の構造遺伝子")] public int armCount;    // 手足の数（★新要素：今回は初期値を2〜4本などでランダムにします）
    [Tooltip("1本の足の中にある関節（セグメント）の数")] public int jointsPerArm;
    [Tooltip("脳の配線遺伝子")]
    public float[] weights; // 脳の全配線の強さ（重み）のデータ

    // 新しく完全ランダムな遺伝子を作る（第1世代用）
    public CreatureGenome(int totalWeightsCount)
    {
        armCount = Random.Range(2, 4); // 2〜3本の手足をランダムに生やす（★新要素）    
        jointsPerArm = Random.Range(1, 3); // 1〜2個の関節をランダムに生やす（★新要素）

        weights = new float[totalWeightsCount];
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = Random.Range(-1f, 1f); // -1.0 〜 +1.0 の間でランダム初期化
        }
    }

    // 親の遺伝子を完全にコピーする
    public CreatureGenome Clone()
    {
        CreatureGenome clone = new CreatureGenome(this.weights.Length);
        clone.armCount = this.armCount;
        clone.jointsPerArm = this.jointsPerArm;
        clone.generation = this.generation + 1;
        System.Array.Copy(this.weights, clone.weights, this.weights.Length);
        return clone;
    }

    // 🔥 突然変異（Mutation）をさせるAPI
    // スマホゲームの「ショップのアップグレード」や「進化」でこの数値をいじります
    public void Mutate(float mutationRate, float mutationAmount)
    {
        for (int i = 0; i < weights.Length; i++)
        {
            // mutationRate（例えば0.1なら10%の確率）で配線が狂う
            if (Random.value < mutationRate)
            {
                weights[i] += Random.Range(-mutationAmount, mutationAmount);
                weights[i] = Mathf.Clamp(weights[i], -1f, 1f); // 数値を行き過ぎないように制限
            }
        }

        if (Random.value < mutationRate)
        {
            int delta = Random.Range(-1, 2); // -1, 0, +1 のどれかで手足の数が変化する
            armCount = Mathf.Clamp(armCount + delta, 0, 15); // 手足は最小0本、最大15本に制限
            
            // Debug.Log($"🧬 身体に突然変異が発生！手足の数が {armCount} 本になりました。");
        }

        if (Random.value < mutationRate)
        {
            int delta = Random.Range(-1, 2); // -1, 0, +1 のどれかで関節の数が変化する
            jointsPerArm = Mathf.Clamp(jointsPerArm + delta, 1, 5); // 関節は最小1つ、最大5つに制限
            // Debug.Log($"🧬 突然変異により、足の関節数が {jointsPerArm} 個になりました！");
        }
    }
}

// 🧠 脳みそクラス：インプットからアウトプットを計算するだけの純粋な計算機
public class CreatureBrain
{
    private int inputCount;
    private int outputCount;
    private CreatureGenome genome;

    // 脳の初期化（入力の数、出力の数を設定）
    public CreatureBrain(int inputs, int outputs)
    {
        inputCount = inputs;
        outputCount = outputs;

        // 簡単な「入力層 ➔ 出力層」の直結ネットワーク（一番軽量でスマホ向き）
        // 必要になる配線の総数は 入力数 × 出力数
        int totalWeights = inputCount * outputCount;
        genome = new CreatureGenome(totalWeights);
    }

    // 外部から遺伝子（設計図）を上書きセットする
    public void LoadGenome(CreatureGenome newGenome)
    {
        this.genome = newGenome.Clone();
    }

    // 現在の遺伝子（設計図）を取得する
    public CreatureGenome GetGenome()
    {
        return genome;
    }

    // ⚙️ 【一番重要】目（入力）のデータから手足のモーター速度（出力）を計算する関数
    public float[] Evaluate(float[] inputs)
    {
        float[] outputs = new float[outputCount];

        int weightIndex = 0;

        // ニューラルネットワークのマトリクス計算
        for (int o = 0; o < outputCount; o++)
        {
            float sum = 0f;
            for (int i = 0; i < inputCount; i++)
            {
                // 入力値 × 遺伝子の配線の強さ をすべて足し合わせる
                sum += inputs[i] * genome.weights[weightIndex];
                weightIndex++;
            }

            // 出力値を -1.0 〜 +1.0 の間に綺麗に変換する（活性化関数 Tanh の代わり）
            outputs[o] = Mathf.Max(-1f, Mathf.Min(1f, sum));
        }

        return outputs;
    }
}