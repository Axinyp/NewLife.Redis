﻿using System.Net.Sockets;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Caching.Clusters;

/// <summary>Redis哨兵</summary>
public class RedisSentinel : RedisReplication
{
    #region 属性
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="redis"></param>
    public RedisSentinel(Redis redis) : base(redis) { }
    #endregion

    #region 方法
    /// <summary>分析主从节点</summary>
    public override void GetNodes()
    {
        var showLog = Nodes == null;
        if (showLog) XTrace.WriteLine("分析[{0}]哨兵节点：", Redis?.Name);

        var rs = Execute(r => r.Execute<String>("INFO", "Sentinel"));
        if (rs.IsNullOrEmpty()) return;

        var rds = Redis as FullRedis;
        var inf = rs.SplitAsDictionary(":", "\r\n");
        var rep = new ReplicationInfo
        {
            Role = rds.Mode
        };
        rep.Load(inf);

        Replication = rep;

        var list = new List<RedisNode>();
        if (rep.Masters != null)
        {
            foreach (var item in rep.Masters)
            {
                if (item.IP.IsNullOrEmpty()) continue;

                var node = new RedisNode
                {
                    Owner = Redis,
                    EndPoint = $"{item.IP}:{item.Port}",
                    Slave = false,
                };
                list.Add(node);
            }
        }
        if (rep.Slaves != null)
        {
            foreach (var item in rep.Slaves)
            {
                if (item.IP.IsNullOrEmpty()) continue;

                var node = new RedisNode
                {
                    Owner = Redis,
                    EndPoint = $"{item.IP}:{item.Port}",
                    Slave = true,
                };
                list.Add(node);
            }
        }

        foreach (var node in list)
        {
            var name = Redis?.Name + "";
            if (!name.IsNullOrEmpty()) name = $"[{name}]";

            if (showLog) XTrace.WriteLine("节点：{0} {1}", node.Slave ? "slave" : "master", node.EndPoint);
        }

        Nodes = list.ToArray();
    }
    #endregion
}