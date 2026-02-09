using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.SSM;
using Constructs;
using Newtonsoft.Json;
using AlarmActions = Amazon.CDK.AWS.CloudWatch.Actions;
using LogGroupProps = Amazon.CDK.AWS.Logs.LogGroupProps;
using Policy = Amazon.CDK.AWS.IAM.Policy;

namespace Bounan.Downloader.AwsCdk;

internal sealed class DownloaderCdkStack : Stack
{
    private const string RuntimeConfigParameterPrefix = "/bounan/downloader/runtime-config";

    internal DownloaderCdkStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        var config = new DownloaderCdkStackConfig(this, "bounan:", "/bounan/downloader/deploy-config/");

        var user = new User(this, "User");

        var image = BuildAndPushWorkerImage(user);

        var videoRegisteredQueue = CreateVideoRegisteredQueue(config, user);
        GrantPermissionsForLambdas(config, user);

        var logGroup = CreateLogGroup();
        SetErrorAlarm(config, logGroup);
        SetNoLogsAlarm(config, logGroup);
        _ = logGroup.GrantWrite(user);

        var accessKey = new CfnAccessKey(this, "AccessKey", new CfnAccessKeyProps { UserName = user.UserName });

        SaveParameter(logGroup, videoRegisteredQueue, config, user);

        Out("Config", JsonConvert.SerializeObject(config));

        string value = $"""
                        WORKER_IMAGE_URI={image.ImageUri};
                        AWS_REGION={Region};
                        AWS_ACCESS_KEY_ID={accessKey.Ref};
                        AWS_SECRET_ACCESS_KEY={accessKey.AttrSecretAccessKey};
                        TELEGRAM_API_ID={config.TelegramAppId};
                        TELEGRAM_API_HASH={config.TelegramAppHash}
                        """;
        Out("dotenv", value);
    }

    private DockerImageAsset BuildAndPushWorkerImage(IGrantable user)
    {
        var dockerImage = new DockerImageAsset(this, "WorkerImage", new DockerImageAssetProps { Directory = "." });
        _ = dockerImage.Repository.GrantPull(user);

        return dockerImage;
    }

    private Queue CreateVideoRegisteredQueue(DownloaderCdkStackConfig config, IGrantable user)
    {
        var newEpisodesTopic = Topic.FromTopicArn(this, "VideoRegisteredTopic", config.VideoRegisteredTopicArn);

        var queueProps = new QueueProps { RetentionPeriod = Duration.Minutes(5) };
        var newEpisodesQueue = new Queue(this, "VideoRegisteredQueue", queueProps);
        _ = newEpisodesTopic.AddSubscription(new SqsSubscription(newEpisodesQueue));

        _ = newEpisodesQueue.GrantConsumeMessages(user);

        return newEpisodesQueue;
    }

    private void GrantPermissionsForLambdas(DownloaderCdkStackConfig config, IGrantable user)
    {
        var functionAttributes = new FunctionAttributes
        {
            FunctionArn = config.LoanApiFunctionArn,
            SkipPermissions = true,
        };
        var loanApiClientLambda = Function.FromFunctionAttributes(this, "LoanApiFunction", functionAttributes);
        _ = loanApiClientLambda.GrantInvoke(user);

        var getAnimeToDownloadLambda = Function.FromFunctionName(
            this,
            "GetAnime",
            config.GetVideoToDownloadLambdaName);
        _ = getAnimeToDownloadLambda.GrantInvoke(user);

        var updateVideoStatusLambda = Function.FromFunctionName(
            this,
            "UpdateVideoStatus",
            config.UpdateVideoStatusLambdaName);
        _ = updateVideoStatusLambda.GrantInvoke(user);
    }

    private LogGroup CreateLogGroup()
    {
        return new LogGroup(this, "LogGroup", new LogGroupProps { Retention = RetentionDays.ONE_WEEK });
    }

    private void SetErrorAlarm(DownloaderCdkStackConfig config, LogGroup logGroup)
    {
        var topic = new Topic(this, "LogGroupAlarmSnsTopic", new TopicProps());

        _ = topic.AddSubscription(new EmailSubscription(config.AlertEmail));

        var metricFilterOptions = new MetricFilterOptions
        {
            FilterPattern = FilterPattern.AnyTerm("[Error]"),
            MetricNamespace = StackName,
            MetricName = "ErrorCount",
            MetricValue = "1",
        };
        var metricFilter = logGroup.AddMetricFilter("ErrorMetricFilter", metricFilterOptions);

        var alarmProps = new AlarmProps
        {
            Metric = metricFilter.Metric(),
            Threshold = 1,
            EvaluationPeriods = 1,
            TreatMissingData = TreatMissingData.NOT_BREACHING,
        };
        var alarm = new Alarm(this, "LogGroupErrorAlarm", alarmProps);
        alarm.AddAlarmAction(new AlarmActions.SnsAction(topic));
    }

    private void SetNoLogsAlarm(DownloaderCdkStackConfig config, LogGroup logGroup)
    {
        var noLogsMetric = new Metric(
            new MetricProps
            {
                Namespace = "AWS/Logs",
                MetricName = "IncomingLogEvents",
                DimensionsMap = new Dictionary<string, string>
                {
                    { "LogGroupName", logGroup.LogGroupName },
                },
                Statistic = "Sum",
                Period = Duration.Minutes(5),
            });

        var alarmProps = new AlarmProps
        {
            Metric = noLogsMetric,
            Threshold = 0,
            ComparisonOperator = ComparisonOperator.LESS_THAN_OR_EQUAL_TO_THRESHOLD,
            EvaluationPeriods = 1,
            TreatMissingData = TreatMissingData.BREACHING,
            AlarmDescription = "Alarm if no logs received within 5 minutes",
        };
        var noLogAlarm = new Alarm(this, "NoLogsAlarm", alarmProps);

        var topic = new Topic(this, "NoLogAlarmSnsTopic", new TopicProps());
        _ = topic.AddSubscription(new EmailSubscription(config.AlertEmail));
        noLogAlarm.AddAlarmAction(new AlarmActions.SnsAction(topic));
    }

    private void SaveParameter(
        LogGroup logGroup,
        Queue videoRegisteredQueue,
        DownloaderCdkStackConfig config,
        User user)
    {
        var runtimeConfig = new
        {
            AniMan = new
            {
                GetVideoToDownloadLambdaFunctionName = config.GetVideoToDownloadLambdaName,
                UpdateVideoStatusLambdaFunctionName = config.UpdateVideoStatusLambdaName,
            },
            Logging = new
            {
                LogGroup = logGroup.LogGroupName,
            },
            Sqs = new
            {
                NotificationQueueUrl = videoRegisteredQueue.QueueUrl,
            },
            Hls2TlgrUploader = new
            {
                Telegram = new
                {
                    BotToken = config.UploadBotToken,
                    DestinationChatId = config.UploadDestinationChatId,
                },
            },
            Thumbnail = new
            {
                BotId = config.ThumbnailBotUsername,
            },
            Threading = new
            {
                Threads = 2,
            },
            Processing = new
            {
                TimeoutSeconds = 600,
                UseLowestQuality = false,
            },
            LoanApi = new
            {
                FunctionArn = config.LoanApiFunctionArn,
            },
        };

        string json = JsonConvert.SerializeObject(runtimeConfig, Formatting.Indented);

        var stringParameterProps = new StringParameterProps
        {
            ParameterName = RuntimeConfigParameterPrefix + "/json",
            StringValue = json,
        };
        _ = new StringParameter(this, "runtime-config", stringParameterProps);

        var policyProps = new PolicyProps
        {
            Statements =
            [
                new PolicyStatement(
                    new PolicyStatementProps
                    {
                        Actions =
                        [
                            "ssm:GetParametersByPath",
                            "ssm:DescribeParameters",
                            "ssm:GetParameter",
                            "ssm:GetParameterHistory",
                            "ssm:GetParameters",
                        ],
                        Resources =
                        [
                            $"arn:aws:ssm:{Region}:{Account}:parameter{RuntimeConfigParameterPrefix}/*",
                        ],
                    }),
            ],
        };
        user.AttachInlinePolicy(new Policy(this, "ParameterPolicy", policyProps));
    }

    private void Out(string key, string value)
    {
        _ = new CfnOutput(this, key, new CfnOutputProps { Value = value });
    }
}
