﻿using NewLife.Caching;
using NewLife.Log;
using NewLife.Serialization;

namespace QueueDemo;

class AckQueue
{
    public static void Start(FullRedis redis)
    {
        var topic = "AckQueue";

        // 独立线程消费
        var source = new CancellationTokenSource();
        Task.Run(() => ConsumeAsync(redis, topic, source.Token));

        // 发布消息
        Public(redis, topic);

        source.Cancel();
    }

    private static void Public(FullRedis redis, String topic)
    {
        var queue = redis.GetReliableQueue<Area>(topic);

        queue.Add(new Area { Code = 110000, Name = "北京市" });
        Thread.Sleep(1000);
        queue.Add(new Area { Code = 310000, Name = "上海市" });
        Thread.Sleep(1000);
        queue.Add(new Area { Code = 440100, Name = "广州市" });
        Thread.Sleep(1000);
    }

    private static async Task ConsumeAsync(FullRedis redis, String topic, CancellationToken token)
    {
        var queue = redis.GetReliableQueue<String>(topic);

        while (!token.IsCancellationRequested)
        {
            var mqMsg = await queue.TakeOneAsync(10);
            if (mqMsg != null)
            {
                var msg = mqMsg.ToJsonEntity<Area>();
                XTrace.WriteLine("Consume {0} {1}", msg.Code, msg.Name);

                queue.Acknowledge(mqMsg);
            }
        }
    }
}