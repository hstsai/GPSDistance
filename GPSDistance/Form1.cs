using System;
using System.Collections; // ArrayList
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Device.Location; // GeoCoordinate
using System.Data.Entity; // ToBindingList()

namespace GPSDistance
{
  public partial class Form1 : Form
  {
    public Form1()
    {
      InitializeComponent();
    }

    // 計算站間距離
    private void button1_Click(object sender, EventArgs e)
    {
      using (var me = new MetroEntities())
      {
        // 取出LineEdge所有Edge
        var edges =
          from edge in me.LineEdge
          join bs in me.車站 on edge.起站編號 equals bs.車站編號
          join es in me.車站 on edge.迄站編號 equals es.車站編號
          select new
          {
            edge.Edge編號,
            經度1 = bs.經度,
            緯度1 = bs.緯度,
            經度2 = es.經度,
            緯度2 = es.緯度
          };

        progressBar1.Minimum = 1;
        progressBar1.Maximum = edges.Count();
        progressBar1.Step = 1;

        // 計算每個Edge的距離
        foreach (var le in edges)
        {
          var gc1 = new GeoCoordinate(Convert.ToDouble(le.緯度1), Convert.ToDouble(le.經度1));
          var gc2 = new GeoCoordinate(Convert.ToDouble(le.緯度2), Convert.ToDouble(le.經度2));
          var edge = me.LineEdge.Find(le.Edge編號);
          edge.距離 = Convert.ToDecimal(gc1.GetDistanceTo(gc2));

          progressBar1.PerformStep();
        }
        me.SaveChanges();
      }

      MessageBox.Show("計算結束!");
    }

    // 取得最短距離節點編號
    private int MinimumDistance(int[] distance, bool[] shortestPathTreeSet, int verticesCount)
    {
      int min = int.MaxValue;
      int minIndex = 0;

      for (int v = 0; v < verticesCount; v++)
      {
        if (shortestPathTreeSet[v] == false && distance[v] <= min)
        {
          min = distance[v];
          minIndex = v;
        }
      }

      return minIndex;
    }

    // Dijkstra演算法+紀錄路徑 
    private void Dijkstra(int[,] graph, int source, int verticesCount, ref int[] distance, ref int[] previous)
    {
      bool[] shortestPathTreeSet = new bool[verticesCount]; // 節點集合

      for (int i = 0; i < verticesCount; i++)
      {
        distance[i] = int.MaxValue; // 初始化距離為最大值
        previous[i] = -1; // 初始化紀錄路徑
        shortestPathTreeSet[i] = false;
      }

      distance[source] = 0;
      for (int count = 0; count < verticesCount - 1; count++)
      {
        int u = MinimumDistance(distance, shortestPathTreeSet, verticesCount);
        shortestPathTreeSet[u] = true; // 加入最短路徑集合

        for (int v = 0; v < verticesCount; v++) // 修正最短路徑
        {
          if (!shortestPathTreeSet[v] &&
            graph[u, v] < int.MaxValue &&
            distance[u] < int.MaxValue &&
            distance[u] + graph[u, v] < distance[v])
          {
            distance[v] = distance[u] + graph[u, v];
            previous[v] = u;
          }
        }
      }
    }

    // 計算最短距離
    private void button2_Click(object sender, EventArgs e)
    {
      using (var me = new MetroEntities())
      {
        // 清空資料表
        me.Database.ExecuteSqlCommand("DELETE FROM Dijkstra");
        me.Database.ExecuteSqlCommand("DELETE FROM Trip");
        me.Database.ExecuteSqlCommand("DELETE FROM TripNode");

        var verticesCount = me.車站.Count();
        int[,] graph = new int[verticesCount, verticesCount];
        var trips = new ArrayList();

        progressBar2.Minimum = 1;
        progressBar2.Maximum = verticesCount;
        progressBar2.Step = 1;

        // 初始化路網
        for (int i = 0; i < verticesCount; i++)
        {
          trips.Add(new List<int>());
          for (int j = 0; j < verticesCount; j++)
          {
            graph[i, j] = int.MaxValue;

            var trip = new Trip();
            trip.起站編號 = i + 1;
            trip.迄站編號 = j + 1;
            me.Trip.Add(trip);
          }
        }
        me.SaveChanges(); // 提早取得Trip編號
        foreach (var edge in me.LineEdge)
        {
          graph[edge.起站編號 - 1, edge.迄站編號 - 1] = Convert.ToInt32(edge.距離);
        }
        foreach (var edge in me.轉乘)
        {
          graph[edge.起站編號 - 1, edge.迄站編號 - 1] = 0;
        }

        int[] distance = new int[verticesCount];
        int[] previous = new int[verticesCount];
        for (int i = 0; i < verticesCount; i++)
        {
          Dijkstra(graph, i, verticesCount, ref distance, ref previous);
          for (int j = i; j < verticesCount; j++)
          {
            var d = new Dijkstra();
            d.起站編號 = i + 1;
            d.迄站編號 = j + 1;
            d.最短距離 = Convert.ToDecimal(distance[j]);
            me.Dijkstra.Add(d);
            d.起站編號 = j + 1;
            d.迄站編號 = i + 1;
            d.最短距離 = Convert.ToDecimal(distance[j]);
            me.Dijkstra.Add(d);
          }

          // 取出最短路徑
          for (int j = verticesCount - 1; j >= 0; j--)
          {
            var trip = (List<int>)trips[j];
            trip.Clear();
            var p = j;
            while (p >= 0)
            {
              trip.Add(p + 1);
              p = previous[p];
            }
            trip.Reverse();

            // 取得Trip編號
            var path = me.Trip.Where((o) => o.起站編號 == i + 1 && o.迄站編號 == j + 1).Single();
            string line = "";
            var tn = from t in trip // 取得線路編號
                     join s in me.車站 on t equals s.車站編號
                     select new { lid = s.線路編號, sid = s.車站編號 };
            var nlist = tn.ToList();
            for (int k = 0, l = 0; k < nlist.Count(); k++)
            {
              if (k == 0 || k == nlist.Count() - 1) // 起迄站不用轉乘
              {
                var te = new TripNode();
                te.Trip編號 = path.Trip編號;
                te.序號 = l++;
                te.車站編號 = nlist[k].sid;
                te.是否轉乘 = false;
                me.TripNode.Add(te);
                line = nlist[k].lid;
              }
              else if (nlist[k].lid != line) // 換線必須轉乘
              {
                var te = new TripNode();
                te.Trip編號 = path.Trip編號;
                te.序號 = l++;
                te.車站編號 = nlist[k - 1].sid;
                te.是否轉乘 = true;
                me.TripNode.Add(te);
                te = new TripNode();
                te.Trip編號 = path.Trip編號;
                te.序號 = l++;
                te.車站編號 = nlist[k].sid;
                te.是否轉乘 = true;
                me.TripNode.Add(te);
                line = nlist[k].lid;
              }
            }
          }

          progressBar2.PerformStep();
          me.SaveChanges();
        }
      }

      MessageBox.Show("計算結束");
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      using (var me = new MetroEntities())
      {
        comboBox3.DataSource = me.線路.ToList();
        comboBox3.DisplayMember = "中文名稱";
        comboBox3.ValueMember = "線路編號";
        comboBox4.DataSource = me.線路.ToList();
        comboBox4.DisplayMember = "中文名稱";
        comboBox4.ValueMember = "線路編號";
      }
    }

    private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
    {
      using (var me = new MetroEntities())
      {
        var 起站 =
          from o in me.車站
          where o.線路編號 == ((線路)comboBox3.SelectedItem).線路編號
          select o;
        comboBox1.DataSource = 起站.ToList();
        comboBox1.DisplayMember = "中文名稱";
        comboBox1.ValueMember = "車站編號";
      }

    }

    private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
    {
      using (var me = new MetroEntities())
      {
        var 迄站 =
          from o in me.車站
          where o.線路編號 == ((線路)comboBox4.SelectedItem).線路編號
          select o;
        comboBox2.DataSource = 迄站.ToList();
        comboBox2.DisplayMember = "中文名稱";
        comboBox2.ValueMember = "車站編號";
      }

    }

    private void button10_Click(object sender, EventArgs e)
    {
      using (var me = new MetroEntities())
      {
        int 起站 = Convert.ToInt32(comboBox1.SelectedValue);
        int 迄站 = Convert.ToInt32(comboBox2.SelectedValue);
        var 起訖距離 =
          (from o in me.Dijkstra
          where o.起站編號 == 起站 && o.迄站編號 == 迄站
          select o).First();
        double time = (double)起訖距離.最短距離 / 1000 / 39.54 * 60; // 平均時速 39.54km/h
        textBox1.Text = time.ToString("F0"); 
        textBox2.Text = "55";
        textBox3.Text = 起訖距離.最短距離.ToString();

        var 起訖路徑 =
          from o in me.Trip
          join p in me.TripNode on o.Trip編號 equals p.Trip編號
          join s in me.車站 on p.車站編號 equals s.車站編號
          join l in me.線路 on s.線路編號 equals l.線路編號
          where o.起站編號 == 起站 && o.迄站編號 == 迄站
          orderby p.Trip編號, p.序號
          select new { 搭乘 = o.Trip編號, 線路 = l.中文名稱, 車站 = s.中文名稱, 轉乘 = p.是否轉乘 };
        textBox4.Text = "";
        long trip = -1;
        int i = 1;
        foreach (var p in 起訖路徑)
        {
          if (p.搭乘 != trip)
          {
            trip = p.搭乘;
            textBox4.Text += "第" + i++ + "種：\r\n";
          }
          textBox4.Text += ((bool)p.轉乘 ? "轉＞" : "搭＞") + p.線路 + "(" + p.車站 + ")\r\n";
        }
        groupBox5.Visible = true;
      }
    }
  }
}
