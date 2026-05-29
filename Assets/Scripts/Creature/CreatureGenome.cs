using UnityEngine;
using Neuro.Creature;

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

    [Header("★脳の構造遺伝子")]
    public int hiddenNodeCount; // 💡 新要素：中間層のノード数（脳のシワの量）

    [Header("脳の配線遺伝子")]
    public float[] weights; // 脳の全配線の強さ（重み）のデータ

    [Header("系統情報")]
    public string lineageId;
    public string parentLineageId;
    public string lineageName;

    // 新しく完全ランダムな遺伝子を作る（第1世代用）
    public CreatureGenome(int totalWeightsCount)
    {
        armCount = Random.Range(2, 4);     
        jointsPerArm = Random.Range(1, 3); 
        sightRange = 5f;        
        fieldOfViewAngle = 90f; 
        
        // 🧠 初期値はちょっとおバカな「4ノード」からスタートさせるっす！
        hiddenNodeCount = 4; 

        weights = new float[totalWeightsCount];
        for (int i = 0; i < weights.Length; i++)
            weights[i] = Random.Range(-1f, 1f); 
    }

    // 親の遺伝子を完全にコピーする
    public CreatureGenome Clone()
    {
        CreatureGenome clone = new CreatureGenome(this.weights != null ? this.weights.Length : 0);
        clone.armCount = this.armCount;
        clone.jointsPerArm = this.jointsPerArm;
        clone.sightRange = this.sightRange;
        clone.fieldOfViewAngle = this.fieldOfViewAngle;
        
        // 🧠 脳のサイズを子に引き継ぐ
        clone.hiddenNodeCount = this.hiddenNodeCount;

        clone.generation = this.generation + 1;
        clone.lineageId = this.lineageId;
        clone.parentLineageId = this.parentLineageId;
        clone.lineageName = this.lineageName;
        if (this.weights != null) System.Array.Copy(this.weights, clone.weights, this.weights.Length);
        return clone;
    }

    public CreatureGenome CopyExact()
    {
        CreatureGenome copy = Clone();
        copy.generation = this.generation;
        return copy;
    }

    // 🔥 突然変異（Mutation）をさせるAPI
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

        // 🧠 ★新要素：脳のシワ（中間層）の突然変異（10%の確率で±1ノード変化する）
        if (Random.value < mutationRate)
        {
            int delta = (Random.value < 0.5f) ? 1 : -1;
            hiddenNodeCount = Mathf.Clamp(hiddenNodeCount + delta, 2, 24); // 最小2、最大24
            Debug.Log($"🧬 突然変異により、生まれつき脳のキャパ（中間層）が {hiddenNodeCount} に変化した天才が誕生！");
        }
    }
}

// 🧠 隠れ層（中間層）のサイズを動的に変更できるようにした新しい脳
public class CreatureBrain
{
    private int inputCount;
    private int outputCount;
    private int hiddenCount; // 💡 遺伝子から可変で受け取る
    private CreatureGenome genome;

    public CreatureBrain(int inputs, int outputs, int hCount)
    {
        inputCount = inputs;
        outputCount = outputs;
        hiddenCount = hCount; // 💡 確定したサイズを入れる

        int totalWeights = (inputCount * hiddenCount) + (hiddenCount * outputCount);
        genome = new CreatureGenome(totalWeights);
        genome.hiddenNodeCount = hiddenCount; // 遺伝子側にも同期
    }

    public void LoadGenome(CreatureGenome newGenome)
    {
        // 💡 遺伝子データ側に記録されている脳のサイズを最優先で適用するっす！
        if (newGenome != null)
        {
            hiddenCount = newGenome.hiddenNodeCount;
        }

        int expectedWeights = (inputCount * hiddenCount) + (hiddenCount * outputCount);

        if (newGenome == null)
        {
            this.genome = new CreatureGenome(expectedWeights);
            this.genome.hiddenNodeCount = hiddenCount;
            return;
        }

        if (newGenome.weights != null && newGenome.weights.Length == expectedWeights)
        {
            this.genome = newGenome.Clone();
            return;
        }

        // 🛠️ 脳のノード数や手足が変わったときに、配線を安全に自動リサイズして引き継ぐ
        CreatureGenome adjusted = new CreatureGenome(expectedWeights);
        adjusted.armCount = newGenome.armCount;
        adjusted.jointsPerArm = newGenome.jointsPerArm;
        adjusted.sightRange = newGenome.sightRange;
        adjusted.fieldOfViewAngle = newGenome.fieldOfViewAngle;
        adjusted.generation = newGenome.generation;
        adjusted.hiddenNodeCount = hiddenCount; // 💡 新しいサイズを確定

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
            hiddenOutputs[h] = Mathf.Max(0f, sum); // ReLU
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
            outputs[o] = Mathf.Max(-1f, Mathf.Min(1f, sum));
        }

        return outputs;
    }
}