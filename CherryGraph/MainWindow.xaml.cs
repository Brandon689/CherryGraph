using Dapper;
using Microsoft.Msagl.Drawing;
using System.Windows;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;

namespace CherryGraph;

public partial class MainWindow : Window
{
    private const string DatabasePath = @"C:\Users\Brandon\Documents\Obsidian Vault\.cherrybomb.ctb~";

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Graph graph = BuildGraphFromDatabase();
        graphControl.Graph = graph;
    }

    private Graph BuildGraphFromDatabase()
    {
        Graph graph = new Graph();
        graph.Attr.LayerDirection = LayerDirection.LR;

        using (var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;"))
        {
            connection.Open();

            // Query to get all nodes
            var nodes = connection.Query<NodeData>("SELECT node_id, name FROM node").ToDictionary(n => n.node_id, n => n);

            // Query to get all parent-child relationships
            var relationships = connection.Query<ChildrenData>("SELECT node_id, father_id FROM children").ToList();

            // Create graph nodes
            foreach (var node in nodes.Values)
            {
                Node graphNode = graph.AddNode(node.node_id.ToString());
                graphNode.LabelText = node.name;
                graphNode.Attr.Shape = GetRandomShape();
            }

            // Create graph edges
            foreach (var rel in relationships)
            {
                if (nodes.ContainsKey(rel.node_id) && nodes.ContainsKey(rel.father_id))
                {
                    graph.AddEdge(rel.father_id.ToString(), rel.node_id.ToString());
                }
            }

            // Determine root nodes (nodes without a parent)
            var childNodes = relationships.Select(r => r.node_id).ToHashSet();
            var rootNodes = nodes.Keys.Except(childNodes);

            // Assign levels to nodes
            AssignLevels(graph, rootNodes);
        }

        return graph;
    }

    private void AssignLevels(Graph graph, IEnumerable<long> nodeIds, int level = 0)
    {
        foreach (var nodeId in nodeIds)
        {
            var node = graph.FindNode(nodeId.ToString());
            if (node != null)
            {
                node.Attr.Shape = GetShapeForLevel(level);

                var childIds = GetChildNodeIds(graph, node);
                if (childIds.Any())
                {
                    AssignLevels(graph, childIds, level + 1);
                }
            }
        }
    }

    private IEnumerable<long> GetChildNodeIds(Graph graph, Node parentNode)
    {
        return graph.Edges
            .Where(e => e.Source == parentNode.Id)
            .Select(e => long.Parse(e.Target));
    }



    private Shape GetShapeForLevel(int level)
    {
        switch (level % 7)
        {
            case 0: return Shape.Box;
            case 1: return Shape.Diamond;
            case 2: return Shape.Circle;
            case 3: return Shape.Hexagon;
            case 4: return Shape.Octagon;
            case 5: return Shape.House;
            case 6: return Shape.InvHouse;
            default: return Shape.Box;
        }
    }

    private Shape GetRandomShape()
    {
        var shapes = new[] { Shape.Box, Shape.Diamond, Shape.Circle, Shape.Hexagon, Shape.Octagon, Shape.House, Shape.InvHouse };
        return shapes[new Random().Next(shapes.Length)];
    }
}

public class NodeData
{
    public long node_id { get; set; }
    public string name { get; set; }
}

public class ChildrenData
{
    public long node_id { get; set; }
    public long father_id { get; set; }
}
