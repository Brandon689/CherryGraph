using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Core.Routing;
using System;
using System.Windows;
using System.Data.SQLite;
using System.Linq;
using Dapper;

namespace CherryGraph
{
    public partial class MainWindow : Window
    {
        private const string DatabasePath = @"C:\Users\Brandon\Documents\Obsidian Vault\.cherrybomb.ctb~";
        private enum LayoutType { Horizontal, Vertical, ForceDirected }
        private LayoutType currentLayout = LayoutType.Horizontal;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DrawGraph();
        }

        private void DrawGraph()
        {
            try
            {
                var graph = new Graph();

                using (var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;"))
                {
                    connection.Open();

                    var nodes = connection.Query<NodeData>("SELECT node_id, name FROM node").ToDictionary(n => n.node_id, n => n);
                    var relationships = connection.Query<ChildrenData>("SELECT node_id, father_id FROM children").ToList();

                    foreach (var node in nodes.Values)
                    {
                        graph.AddNode(node.node_id.ToString()).LabelText = node.name;
                    }

                    foreach (var rel in relationships)
                    {
                        if (nodes.ContainsKey(rel.node_id) && nodes.ContainsKey(rel.father_id))
                        {
                            graph.AddEdge(rel.father_id.ToString(), rel.node_id.ToString());
                        }
                    }
                }

                ApplyLayout(graph);

                graphControl.Graph = graph;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error drawing graph: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyLayout(Graph graph)
        {
            switch (currentLayout)
            {
                case LayoutType.Horizontal:
                    graph.Attr.LayerDirection = LayerDirection.LR;
                    graph.LayoutAlgorithmSettings = new SugiyamaLayoutSettings
                    {
                        EdgeRoutingSettings = new EdgeRoutingSettings { EdgeRoutingMode = EdgeRoutingMode.Rectilinear }
                    };
                    break;

                case LayoutType.Vertical:
                    graph.Attr.LayerDirection = LayerDirection.TB;
                    graph.LayoutAlgorithmSettings = new SugiyamaLayoutSettings
                    {
                        EdgeRoutingSettings = new EdgeRoutingSettings { EdgeRoutingMode = EdgeRoutingMode.Rectilinear }
                    };
                    break;

                case LayoutType.ForceDirected:
                    graph.LayoutAlgorithmSettings = new MdsLayoutSettings
                    {
                        EdgeRoutingSettings = new EdgeRoutingSettings { EdgeRoutingMode = EdgeRoutingMode.Spline },
                       // AdjustLayout = true,
                        //MaxIterations = 50,
                       // RepulsiveForceConstant = 2.0,
                        //SpringConstant = 0.5
                    };
                    break;
            }
        }

        private void ToggleLayout_Click(object sender, RoutedEventArgs e)
        {
            currentLayout = (LayoutType)(((int)currentLayout + 1) % 3);
            DrawGraph();
        }
    }

}
