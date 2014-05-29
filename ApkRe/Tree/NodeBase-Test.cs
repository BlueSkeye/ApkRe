using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkRe.Tree
{
    public class TestTreeNode : NodeBase<TestTreeNode>
    {
        #region CONSTRUCTORS
        internal TestTreeNode(string label)
        {
            Label = label;
            return;
        }

        internal TestTreeNode(string label, TestTreeNode parent)
            : this(label)
        {
            parent.AddSon(this);
            return;
        }
        #endregion

        #region PROPERTIES
        internal string Label { get; private set; }
        #endregion

        #region METHODS
        private static void DiagnoseFailedTest(TestContext context)
        {
            Console.WriteLine();
            Console.WriteLine("{0} test FAILED.", context.Label);
            Console.WriteLine("Expected : {0}", context.Expected);
            Console.WriteLine("Received : {0}", context.ResultBuilder.ToString());
            throw new ApplicationException();
        }

        public static bool DoTests()
        {
            Dictionary<string, TestTreeNode> tree = InitializeTestTree();

            if (!TestWalk("Sons then father till end", tree["root"], SimpleHandler, WalkMode.SonsThenFather, true,
                "cNA11|cNA12|cNA13|cNA1|cNA2|cNA|cNB11|cNB1|cNB2|cNB311|cNB31|cNB32|cNB3|cNB|cNroot|<EOT>"))
            { return false; }
            if (!TestWalk("Sons then father (R2L) till end", tree["root"], SimpleHandler, WalkMode.SonsThenFather, false,
                "cNB32|cNB311|cNB31|cNB3|cNB2|cNB11|cNB1|cNB|cNA2|cNA13|cNA12|cNA11|cNA1|cNA|cNroot|<EOT>"))
            { return false; }
            if (!TestWalk("Simple transit till end", tree["root"], SimpleHandler, WalkMode.TransitBeforeAndAfter, true,
                "bNroot|bNA|bNA1|cNA11|cNA12|cNA13|aNA1|cNA2|aNA|bNB|bNB1|cNB11|aNB1|cNB2|bNB3|bNB31|cNB311|aNB31|cNB32|aNB3|aNB|aNroot|<EOT>"))
            { return false; }
            if (!TestWalk("Simple transit (R2L) till end", tree["root"], SimpleHandler, WalkMode.TransitBeforeAndAfter, false,
                "bNroot|bNB|bNB3|cNB32|bNB31|cNB311|aNB31|aNB3|cNB2|bNB1|cNB11|aNB1|aNB|bNA|cNA2|bNA1|cNA13|cNA12|cNA11|aNA1|aNA|aNroot|<EOT>"))
            { return false; }
            if (!TestWalk("Full transit till end", tree["root"], SimpleHandler, WalkMode.FullTransit, true,
                "bNroot|bNA|bNA1|cNA11|tNA1|cNA12|tNA1|cNA13|aNA1|tNA|cNA2|aNA|tNroot|bNB|bNB1|cNB11|aNB1|tNB|cNB2|tNB|bNB3|bNB31|cNB311|aNB31|tNB3|cNB32|aNB3|aNB|aNroot|<EOT>"))
            { return false; }
            return true;
        }

        private static WalkContinuation SimpleHandler(NodeBase<TestTreeNode> node, WalkTraversal traversal,
            object context)
        {
            TestContext testContext = (TestContext)context;
            string label = "";
            if (0 == testContext.ResultBuilder.Length) {
                Console.WriteLine("TEST {0} :", testContext.Label);
            }
            int resultBuilderLength = testContext.ResultBuilder.Length;
            if ((resultBuilderLength + 2) >= testContext.Expected.Length) {
                DiagnoseFailedTest(testContext);
            }
            char expectedTraversal = ' ';
            char nextAction = 'N';

            if (null == node) { testContext.ResultBuilder.Append("<EOT>"); }
            else {
                expectedTraversal = testContext.Expected[resultBuilderLength];
                nextAction = testContext.Expected[resultBuilderLength + 1];

                char realTraversal;
                switch (traversal)
                {
                    case WalkTraversal.AfterTransit:
                        realTraversal = 'a';
                        break;
                    case WalkTraversal.BeforeTransit:
                        realTraversal = 'b';
                        break;
                    case WalkTraversal.CurrentNode:
                        realTraversal = 'c';
                        break;
                    case WalkTraversal.Transit:
                        realTraversal = 't';
                        break;
                    default:
                        realTraversal = 'X';
                        break;
                }
                testContext.ResultBuilder.Append(realTraversal);
                testContext.ResultBuilder.Append(nextAction);
                label = ((TestTreeNode)node).Label;
                testContext.ResultBuilder.Append(label);
            }
            if (null != node) { testContext.ResultBuilder.Append("|"); }
            // Console.WriteLine(testContext.ResultBuilder.ToString());
            Console.Write(".");
            // if ("bNroot|bNA|bNA1|cNA11|cNA12|cNA13|aNA1|cNA2|aNA|bNB|bNB1|cNB11|aNB1|cNB2|bNB3|bNB31|cNB311|aNB31|cNB32|aNB3|" == testContext.ResultBuilder.ToString()) { int i = 1; }
            if (!testContext.Expected.StartsWith(testContext.ResultBuilder.ToString())) {
                DiagnoseFailedTest(testContext);
            }
            switch (nextAction)
            {
                case 'N':
                    return WalkContinuation.Normal;
                case 'B':
                    return WalkContinuation.SkipBrothers;
                case 'S':
                    return WalkContinuation.SkipSons;
                case 'T':
                    return WalkContinuation.Terminate;
                default:
                    Console.WriteLine();
                    Console.WriteLine("Invalid expected action {0}", nextAction);
                    return WalkContinuation.Normal;
            }
        }

        private static bool TestWalk(string testLabel, TestTreeNode startNode,
            WalkNodeHandlerDelegate nodeHandler, WalkMode mode, bool leftToRightSons,
            string expectedResult)
        {
            TestContext context = new TestContext(testLabel, expectedResult);

            try {
                startNode.Walk(nodeHandler, mode, context, leftToRightSons);
                Console.WriteLine();
                Console.WriteLine("Test succeeded.");
                return true;
            }
            catch { return false; }
        }

        // 

        /// <summary>Create a test tree that looks like this :
        /// root
        ///   A
        ///     A1
        ///       A11
        ///       A12
        ///       A13
        ///     A2
        ///   B
        ///     B1
        ///       B11
        ///     B2
        ///     B3
        ///       B31
        ///         B311
        ///       B32
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, TestTreeNode> InitializeTestTree()
        {
            Dictionary<string, TestTreeNode> nodesByName = new Dictionary<string, TestTreeNode>();
            nodesByName["root"] = new TestTreeNode("root");
              nodesByName["A"] = new TestTreeNode("A", nodesByName["root"]);
                nodesByName["A1"] = new TestTreeNode("A1", nodesByName["A"]);
                  nodesByName["A11"] = new TestTreeNode("A11", nodesByName["A1"]);
                  nodesByName["A12"] = new TestTreeNode("A12", nodesByName["A1"]);
                  nodesByName["A13"] = new TestTreeNode("A13", nodesByName["A1"]);
                nodesByName["A2"] = new TestTreeNode("A2", nodesByName["A"]);
              nodesByName["B"] = new TestTreeNode("B", nodesByName["root"]);
                nodesByName["B1"] = new TestTreeNode("B1", nodesByName["B"]);
                  nodesByName["B11"] = new TestTreeNode("B11", nodesByName["B1"]);
                nodesByName["B2"] = new TestTreeNode("B2", nodesByName["B"]);
                nodesByName["B3"] = new TestTreeNode("B3", nodesByName["B"]);
                  nodesByName["B31"] = new TestTreeNode("B31", nodesByName["B3"]);
                    nodesByName["B311"] = new TestTreeNode("B311", nodesByName["B31"]);
                  nodesByName["B32"] = new TestTreeNode("B32", nodesByName["B3"]);
            
            return nodesByName;
        }
        #endregion

        private class TestContext
        {
            internal TestContext(string label, string expected)
            {
                Label = label;
                Expected = expected;
                ResultBuilder = new StringBuilder();
            }

            internal string Label { get; private set; }
            internal string Expected { get; private set; }
            internal StringBuilder ResultBuilder { get; private set; }
        }
    }
}
