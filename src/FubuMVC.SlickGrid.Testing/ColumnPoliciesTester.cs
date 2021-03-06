﻿using FubuCore.Reflection;
using NUnit.Framework;
using FubuTestingSupport;

namespace FubuMVC.SlickGrid.Testing
{
    [TestFixture]
    public class ColumnPoliciesTester
    {
        [Test]
        public void adding_rules()
        {
            var policies = new ColumnPolicies();
            policies.If(a => a.Name == "Name").SetProperty("foo", "bar");
            policies.If(a => a.Name == "Name").SetProperty("foo1", "bar1");

            var accessor1 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.Name);
            var accessor2 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.IsCool);

            policies.RulesFor(accessor1).ShouldHaveCount(2);
            policies.RulesFor(accessor2).ShouldHaveCount(0);
        }

        

        [Test]
        public void adding_filters()
        {
            var accessor1 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.Name);
            var accessor2 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.IsCool);

            var policies = new ColumnPolicies();
            policies.If(a => a.Name == "Name").EditWith(new SlickGridEditor("foo"));

            policies.EditorFor(accessor1).ShouldEqual(new SlickGridEditor("foo"));
            policies.EditorFor(accessor2).ShouldEqual(SlickGridEditor.Text);
        }

        [Test]
        public void first_filter_wins()
        {
            var accessor1 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.Name);
            var accessor2 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.IsCool);

            var policies = new ColumnPolicies();
            policies.If(a => a.Name == "Name").EditWith(new SlickGridEditor("foo"));
            policies.If(a => a.OwnerType == typeof (GridDefinitionTester.GridDefTarget)).EditWith(new SlickGridEditor("bar"));

            policies.EditorFor(accessor1).ShouldEqual(new SlickGridEditor("foo"));
            policies.EditorFor(accessor2).ShouldEqual(new SlickGridEditor("bar"));
        }


        [Test]
        public void element_selection_matches()
        {
            var accessor1 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.Name);
            var accessor2 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.IsCool);

            var selection = new EditorSelection(a => a.Name == "Name", SlickGridEditor.Underscore);

            selection.Matches(accessor1).ShouldBeTrue();
            selection.Matches(accessor2).ShouldBeFalse();
        }

        [Test]
        public void element_selection_select_is_straight_passthrough()
        {
            var accessor1 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.Name);
            var selection = new EditorSelection(a => a.Name == "Name", SlickGridEditor.Underscore);

            selection.EditorFor(accessor1).ShouldEqual(SlickGridEditor.Underscore);
        }


        [Test]
        public void adding_filters_for_selector()
        {
            var accessor1 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.Name);
            var accessor2 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.IsCool);

            var policies = new ColumnPolicies();
            policies.If(a => a.Name == "Name").FormatWith(new SlickGridFormatter("foo"));

            policies.FormatterFor(accessor1).ShouldEqual(new SlickGridFormatter("foo"));
            policies.FormatterFor(accessor2).ShouldBeNull();
        }

        [Test]
        public void first_filter_wins_with_formatters()
        {
            var accessor1 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.Name);
            var accessor2 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.IsCool);

            var policies = new ColumnPolicies();
            policies.If(a => a.Name == "Name").FormatWith(new SlickGridFormatter("foo"));
            policies.If(a => a.OwnerType == typeof(GridDefinitionTester.GridDefTarget)).FormatWith(new SlickGridFormatter("bar"));

            policies.FormatterFor(accessor1).ShouldEqual(new SlickGridFormatter("foo"));
            policies.FormatterFor(accessor2).ShouldEqual(new SlickGridFormatter("bar"));
        }


        [Test]
        public void element_selection_matches_with_formatters()
        {
            var accessor1 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.Name);
            var accessor2 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.IsCool);

            var selection = new FormatterSelection(a => a.Name == "Name", SlickGridFormatter.Underscore);

            selection.Matches(accessor1).ShouldBeTrue();
            selection.Matches(accessor2).ShouldBeFalse();
        }

        [Test]
        public void element_selection_select_is_straight_passthrough_with_formatters()
        {
            var accessor1 = ReflectionHelper.GetAccessor<GridDefinitionTester.GridDefTarget>(x => x.Name);
            var selection = new FormatterSelection(a => a.Name == "Name", SlickGridFormatter.Underscore);

            selection.FormatterFor(accessor1).ShouldEqual(SlickGridFormatter.Underscore);
        }
    }
}