using UnityEngine;
using Neuro.Creature;

// 🧬 遺伝子（ゲノム）クラス
[System.Serializable]
public class CreatureGenome
{
    [Tooltip("何世代目か")]
    public int generation = 1;
    
    [Header("身体の構造遺伝子")] 
    public int armCount;    
    public int jointsPerArm;

    [Header("感覚の構造遺伝子")]
    public float sightRange;       
    public float fieldOfViewAngle; 

    [Header("脳の配線遺伝子")]
    public float[] weights; // 脳の全配線の強さ（重み）のデータ

    public CreatureGenome(int totalWeightsCount)
    {
        armCount = Random.Range(2, 4);     
        jointsPerArm = Random.Range(1, 3); 
        sightRange = 5f;        
        fieldOfViewAngle = 90f; 

        weights = new float[totalWeightsCount];
        for (int i = 0; i < weights.Length; i++)
            weights[i] = Random.Range(-1f, 1f); 
    }

    public CreatureGenome Clone()
    {
        CreatureGenome clone = new CreatureGenome(this.weights.Length);
        clone.armCount = this.armCount;
        clone.jointsPerArm = this.jointsPerArm;
        clone.sightRange = this.sightRange;
        clone.fieldOfViewAngle = this.fieldOfViewAngle;
        clone.generation = this.generation + 1;
        System.Array.Copy(this.weights, clone.weights, this.weights.Length);
        return clone;
    }

    public void Mutate(float mutationRate, float mutationAmount)
    {
        for (int i = 0; i < weights.Length; i++)
        {
            if (Random.value < mutationRate)
            {
                weights[i] += Random.Range(-mutationAmount, mutationAmount);
                weights[i] = Mathf.Clamp(weights[i], -1f, 1f); 
            }
        }

        if (Random.value < mutationRate)
        {
            int delta = Random.Range(-1, 2); 
            armCount = Mathf.Clamp(armCount + delta, CreatureLimits.MinArms, CreatureLimits.MaxArms);
        }

        if (Random.value < mutationRate)
        {
            int delta = Random.Range(-1, 2); 
            jointsPerArm = Mathf.Clamp(jointsPerArm + delta, CreatureLimits.MinJointsPerArm, CreatureLimits.MaxJointsPerArm);
        }

        if (Random.value < mutationRate)
            sightRange = Mathf.Clamp(sightRange + Random.Range(-0.5f, 0.5f), 2f, 15f);

        if (Random.value < mutationRate)
            fieldOfViewAngle = Mathf.Clamp(fieldOfViewAngle + Random.Range(-10f, 10f), 30f, 180f);
    }
}

// 🧠 【大進化】隠れ層（中間層）を搭載した多層ニューラルネットワーク脳
public class CreatureBrain
{
    private int inputCount;
    private int outputCount;
    private int hiddenCount = 12; // 💡 中間層のノード数（複雑なジャンプ運動の学習に最適なサイズ）
    private CreatureGenome genome;

    public CreatureBrain(int inputs, int outputs)
    {
        inputCount = inputs;
        outputCount = outputs;

        // 💡 【重要】多層ネットワークに必要な配線総数を計算
        // (入力 ➔ 中間) + (中間 ➔ 出力) 
        int totalWeights = (inputCount * hiddenCount) + (hiddenCount * outputCount);
        genome = new CreatureGenome(totalWeights);
    }

    public void LoadGenome(CreatureGenome newGenome)
    {
        int expectedWeights = (inputCount * hiddenCount) + (hiddenCount * outputCount);

        if (newGenome == null)
        {
            this.genome = new CreatureGenome(expectedWeights);
            return;
        }

        if (newGenome.weights != null && newGenome.weights.Length == expectedWeights)
        {
            this.genome = newGenome.Clone();
            return;
        }

        // 🛠️ 手足や関節が増減しても、古い脳の配線を安全に引き継ぐオートマジックリサイズ
        CreatureGenome adjusted = new CreatureGenome(expectedWeights);
        adjusted.armCount = newGenome.armCount;
        adjusted.jointsPerArm = newGenome.jointsPerArm;
        adjusted.sightRange = newGenome.sightRange;
        adjusted.fieldOfViewAngle = newGenome.fieldOfViewAngle;
        adjusted.generation = newGenome.generation;

        if (newGenome.weights != null)
        {
            int copyLen = Mathf.Min(newGenome.weights.Length, adjusted.weights.Length);
            System.Array.Copy(newGenome.weights, adjusted.weights, copyLen);
            for (int i = copyLen; i < adjusted.weights.Length; i++)
                adjusted.weights[i] = Random.Range(-1f, 1f);
        }

        this.genome = adjusted;
    }

    public CreatureGenome GetGenome() => genome;

    // ⚙️ 【脳の思考ロジック】多層フォワードプロパゲーション
    public float[] Evaluate(float[] inputs)
    {
        // --- 1層目: 入力層 ➔ 中間層 ---
        float[] hiddenOutputs = new float[hiddenCount];
        int wIndex = 0;

        for (int h = 0; h < hiddenCount; h++)
        {
            float sum = 0f;
            for (int i = 0; i < inputCount; i++)
            {
                sum += inputs[i] * genome.weights[wIndex++];
            }
            // 活性化関数: 軽量なReLU（0以下をカット）を挟むことで、脳に「複雑な条件分岐」の能力を与えるっす！
            hiddenOutputs[h] = Mathf.Max(0f, sum); 
        }

        // --- 2層目: 中間層 ➔ 出力層 ---
        float[] outputs = new float[outputCount];
        for (int o = 0; o < outputCount; o++)
        {
            float sum = 0f;
            for (int h = 0; h < hiddenCount; h++)
            {
                sum += hiddenOutputs[h] * genome.weights[wIndex++];
            }
            // 最終出力はモーター速度（-1.0 〜 +1.0）
            outputs[o] = Mathf.Max(-1f, Mathf.Min(1f, sum));
        }

        return outputs;
    }
}