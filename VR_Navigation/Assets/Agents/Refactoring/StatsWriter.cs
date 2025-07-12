using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Globalization;

public static class StatsWriter
{
    [Header("If you want to save the statistics of this run please enter its ID")]
    public static string runID = "";
    private static string folderPath;
    private static string envPath;
    static StreamWriter writer;
    static StreamWriter pedWriter;
    static StreamWriter userWriter;
    private static bool isTraining;
    private static float initialTime = Time.time;

    public static void setupCurriculum(String runId, String env, float minScore, Curriculum curriculum)
    {
        runID = runId;
        isTraining = true;
        if (runId != "")
        {
            CheckFolder();
            WriteGeneralSetting(curriculum);
        }
    }

    public static void setupTesting(String runId)
    {
        runID = runId;
        isTraining = false;
        if (runId != "")
        {
            CheckFolder();
            ResetFolder(folderPath + "/TestLog");
        }

    }
    public static void WriteAgentStats(float x, float z, Group group, float desiredSpeed, float realSpeed, float angle, string envID, string ID, int iteraction)
    {
        if (!isTraining && runID != "")
        {
            writer = new StreamWriter(envPath + "/AgentStats.txt", true);
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            double cur_time = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
            writer.WriteLine(DateTime.Now + ";" + cur_time + ";" + (int)(Time.time - initialTime) + ";" + x + ";" + z + ";" + group.ToString() + ";" + desiredSpeed + ";"+ realSpeed + ";" + angle + ";" + envID + ";" + ID + ";" + iteraction);
            writer.Close();           
        }
    }
    public static void WritePedPyStats(float x, float y, float z, int ID)
    {
        string pedPyPath = Application.dataPath + "/PedPy";
        pedWriter = new StreamWriter(pedPyPath + "/PedPyStats.txt", true);
        pedWriter.WriteLine(ID + "\t" + (int)((Time.time - initialTime) * 100) + "\t" + (x * 50).ToString("N", CultureInfo.InvariantCulture) + "\t" + (z * 50).ToString("N", CultureInfo.InvariantCulture));
        pedWriter.Close();
    }

    public static void WriteUserStats(float x, float z, Quaternion rotation)
    {
        string pedPyPath = Application.dataPath + "/PedPy";
        userWriter = new StreamWriter(pedPyPath + "/UserStats.txt", true);
        userWriter.WriteLine(0 + "\t" + (int)((Time.time - initialTime) * 100) + "\t" + (x * 50).ToString("N", CultureInfo.InvariantCulture) + "\t" + (z * 50).ToString("N", CultureInfo.InvariantCulture) + "\t" + rotation.ToString());
        userWriter.Close();
    }

    public static void WriteEnvStats(Group group, int episodeDuration)
    {
        if (!isTraining && runID != "")
        {
            writer = new StreamWriter(envPath + "/EnvStats.txt", true);
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            double cur_time = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
            writer.WriteLine(DateTime.Now + ";" + cur_time + ";" + episodeDuration + ";" + group.ToString());
            writer.Close();
        }
    }
    public static void WriteEnvTimeStats( int episodeDuration)
    {
        if (!isTraining && runID != "")
        {
            writer = new StreamWriter(envPath + "/EnvTimeStats.txt", true);
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            double cur_time = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
            writer.WriteLine(DateTime.Now + ";" + cur_time + ";" + episodeDuration);
            writer.Close();
        }
    }
    public static void WriteAgentCollision(float x, float z, string tag, string pos, string ID)
    {
        if (!isTraining && runID != "")
        {
            writer = new StreamWriter(envPath + "/AgentCollision.txt", true);
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            double cur_time = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
            writer.WriteLine(DateTime.Now + ";" + cur_time + ";" + (int)(Time.time - initialTime) + ";" + x + ";" + z + ";" + tag + ";" + pos + ";" + ID);
            writer.Close();
        }
    }

    public static void WriteChangeEnv(string envName, float minScore, string phase)
    {
        if (isTraining && runID != "")
        {

            writer = new StreamWriter(folderPath + "/EnvironmentChanges.txt", true);
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            double cur_time = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
            writer.WriteLine(DateTime.Now + ";" + cur_time + ";" + (int)(Time.time - initialTime) + ";" + envName + ";" + minScore + ";" + phase);
            writer.Close();
        }

    }
    public static void WriteEnvRewards(float agentNumber, float reward)
    {
        if (!isTraining && runID != "")
        {
            writer = new StreamWriter(envPath + "/EnvRewards.txt", true);
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            double cur_time = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
            writer.WriteLine(DateTime.Now + ";" + cur_time + ";" + (int)(Time.time - initialTime) + ";" + agentNumber + ";" + reward);
            writer.Close();
        }
    }

    public static void WriteGeneralSetting(Curriculum curriculum)
    {
        if (isTraining && runID != "")
        {

            writer = new StreamWriter(folderPath + "/GeneralSetting.txt", true);
            writer.WriteLine("RunName: " + runID);
            writer.WriteLine("Start at : " + DateTime.Now);
            writer.WriteLine("General rewards: ");
            writer.WriteLine("step_reward: " + MyConstants.step_reward);
            writer.WriteLine("step_finished_reward: " + MyConstants.step_finished_reward);
            writer.WriteLine("finale_target_reward: " + MyConstants.finale_target_reward);
            writer.WriteLine("already_taken_target_reward: " + MyConstants.already_taken_target_reward);
            writer.WriteLine("not_watching_target_reward: " + MyConstants.not_watching_target_reward);
            writer.WriteLine("");

            writer.WriteLine("viewAngle: " + MyConstants.viewAngle);
            writer.WriteLine("rayLength: " + MyConstants.rayLength);
            writer.WriteLine("numberOfRaysPerSide: " + MyConstants.numberOfRaysPerSide);
            writer.WriteLine("");

            writer.WriteLine("speedMaxRange: " + MyConstants.speedMaxRange);
            writer.WriteLine("");

            writer.WriteLine("angleRange: " + MyConstants.angleRange);
            writer.WriteLine("");

            writer.WriteLine("MAXIMUM_VIEW_DISTANCE: " + MyConstants.MAXIMUM_VIEW_DISTANCE);
            writer.WriteLine("MAXIMUM_VIEW_OTHER_AGENTS_DISTANCE: " + MyConstants.MAXIMUM_VIEW_OTHER_AGENTS_DISTANCE);
            writer.WriteLine("");

            writer.WriteLine("rayOffset: " + MyConstants.rayOffset);
            writer.WriteLine("");

            writer.WriteLine("proxemic_small_distance: " + MyConstants.proxemic_small_distance);
            writer.WriteLine("proxemic_small_wall_reward: " + MyConstants.proxemic_small_wall_reward);
            writer.WriteLine("proxemic_small_agent_reward: " + MyConstants.proxemic_small_agent_reward);
            writer.WriteLine("");

            writer.WriteLine("proxemic_medium_ray: " + MyConstants.proxemic_medium_ray);
            writer.WriteLine("proxemic_medium_agent_reward: " + MyConstants.proxemic_medium_agent_reward);
            writer.WriteLine("");

            writer.WriteLine("proxemic_small_ray: " + MyConstants.proxemic_small_ray);
            writer.WriteLine("proxemic_large_agent_reward: " + MyConstants.proxemic_large_agent_reward);
            writer.WriteLine("");
            writer.WriteLine("");
            writer.WriteLine("Training");
            writer.WriteLine("");
            writer.WriteLine("Numero Ambienti contemporanei " + (curriculum.maxI * curriculum.maxJ));
            foreach (var cv in curriculum.curriculumList.Environments)
            {
                writer.WriteLine("Environment: " + cv.envGameObject);
                writer.WriteLine("MinScore: " + cv.minScore);
                writer.WriteLine("MaxEpisode: " + cv.maxEpisodes);
            }
            writer.WriteLine("");
            writer.WriteLine("Retrain");
            writer.WriteLine("Infinite Retrain " + curriculum.infiniteRetrain);

            foreach (var cv in curriculum.retrainCurriculumList.Environments)
            {
                writer.WriteLine("Environment: " + cv.envGameObject);
                writer.WriteLine("MinScore: " + cv.minScore);
                writer.WriteLine("MaxEpisode: " + cv.maxEpisodes);
            }
            writer.WriteLine("");
            writer.WriteLine("Consolidation");
            foreach (var cv in curriculum.consolidatioCurriculumList.Environments)
            {
                writer.WriteLine("Environment: " + cv.envGameObject);
                writer.WriteLine("MinScore: " + cv.minScore);
                writer.WriteLine("MaxEpisode: " + cv.maxEpisodes);
            }

            writer.Close();
        }

    }
    private static void CheckFolder()
    {
        if (runID != "")
        {
            string mainFolderName = Path.Combine(Application.dataPath, "Stats");
            folderPath = Path.Combine(mainFolderName, runID);
            string ScreenPath = Path.Combine(mainFolderName, "Screenshot Env");
            string dateTimeString = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            folderPath = folderPath + "_" + dateTimeString;
            string tensorboardPath = Path.Combine(folderPath, "TensorBoard");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                Directory.CreateDirectory(tensorboardPath);
            }
            if (!Directory.Exists(ScreenPath))
            {
                Directory.CreateDirectory(ScreenPath);
            }
        }

    }
    private static void CheckFolderEnv(string envName)
    {
        var testPath = Path.Combine(folderPath, "TestLog");
        envPath = Path.Combine(testPath, envName);
        if (!Directory.Exists(envPath))
        {
            Directory.CreateDirectory(envPath);
        }
    }

    private static void ResetFolder(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    public static void ChangeEnvSetup(string env)
    {
        if (runID != "")
        {
            CheckFolderEnv(env);
        }
    }

}
