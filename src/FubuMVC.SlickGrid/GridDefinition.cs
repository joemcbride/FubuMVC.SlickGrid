using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using FubuCore.Reflection;
using FubuCore.Util;
using FubuMVC.Core;
using FubuMVC.Core.Registration.Nodes;
using FubuMVC.Core.Registration.Routes;
using FubuMVC.Core.UI.Security;
using FubuMVC.Core.UI.Templates;
using FubuMVC.Core.Urls;
using FubuMVC.Media.Projections;
using FubuCore;
using FubuMVC.Core.Resources.Conneg;

namespace FubuMVC.SlickGrid
{
    public abstract class GridDefinition<T> : IGridDefinition<T>, IFubuRegistryExtension
    {
        private readonly IList<IGridColumn> _columns = new List<IGridColumn>();
        private Type _queryType;
        private Type _sourceType;

        protected GridDefinition()
        {
            Projection = new Projection<T>();
        }

        public Type SourceType
        {
            get { return _sourceType; }
        }

        public AddExpression Add
        {
            get { return new AddExpression(this); }
        }

        void IFubuRegistryExtension.Configure(FubuRegistry registry)
        {
            registry.Configure(graph => {
                Type runnerType = this.As<IGridDefinition>().DetermineRunnerType();
                if (runnerType == null) return;

                MethodInfo method = runnerType.GetMethod("Run");

                var call = new ActionCall(runnerType, method);
                var chain = new BehaviorChain();
                chain.AddToEnd(call);
                chain.Route = new RouteDefinition(DiagnosticConstants.UrlPrefix);
                chain.Route.Append("_data");
                chain.Route.Append(typeof (T).Name);

                chain.MakeAsymmetricJson();

                graph.AddChain(chain);
            });
        }

        public Projection<T> ToProjection(IFieldAccessService accessService)
        {
            Func<Accessor, bool> filter = a => !accessService.RightsFor(null, a.InnerProperty).Equals(AccessRight.None);
            return Projection.Filter(filter);
        }

        string IGridDefinition.ToColumnJson(IFieldAccessService accessService)
        {
            var rights =
                new Cache<IGridColumn, AccessRight>(col => accessService.RightsFor(null, col.Accessor.InnerProperty));

            var builder = new StringBuilder();
            builder.Append("[");
            var columns = _columns.Where(col => !rights[col].Equals(AccessRight.None)).OrderByDescending(x => x.IsFrozen).ToList();

            for (var i = 0; i < columns.Count - 1; i++)
            {
                var column = columns[i];
                column.WriteColumn(builder, rights[column]);
                builder.Append(", ");
            }

            var lastColumn = columns.Last();
            lastColumn.WriteColumn(builder, rights[lastColumn]);

            builder.Append("]");

            return builder.ToString();
        }

        string IGridDefinition.SelectDataSourceUrl(IUrlRegistry urls)
        {
            if (_sourceType == null) return null;

            if (_queryType != null)
            {
                return urls.UrlFor(_queryType);
            }

            Type runnerType = this.As<IGridDefinition>().DetermineRunnerType();

            return urls.UrlFor(runnerType);
        }

        public bool UsesHtmlConventions { get; set; }

        void IGridDefinition.SelectFormattersAndEditors(IColumnPolicies editors)
        {
            _columns.Each(x => x.SelectFormatterAndEditor(this, editors));
        }

        void IGridDefinition.WriteAnyTemplates(ITemplateWriter writer)
        {
            _columns.Each(x => x.WriteTemplates(writer));
        }

        public bool AllColumnsAreEditable { get; set; }
        public SlickGridFormatter DefaultFormatter { get; set; }
        public bool IsPaged()
        {
            if (_queryType == null) return false;

            return _queryType.CanBeCastTo<PagedQuery>();
        }

        public Projection<T> Projection { get; private set; }

        /// <summary>
        /// Source type must implement either IGridDataSource<T> or IGridDataSource<T, TQuery>
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        public void SourceIs<TSource>()
        {
            Type sourceType = typeof (TSource);
            var templateType = sourceType.FindInterfaceThatCloses(typeof (IGridDataSource<>));
            if (templateType != null)
            {
                if (templateType.GetGenericArguments().First() != typeof (T))
                {
                    throw new ArgumentOutOfRangeException("Wrong type as the argument to IGridDataSource<>");
                }

                _queryType = null;
                _sourceType = sourceType;

                return;
            }

            templateType = sourceType.FindInterfaceThatCloses(typeof (IGridDataSource<,>));
            if (templateType != null)
            {
                if (templateType.GetGenericArguments().First() != typeof (T))
                {
                    throw new ArgumentOutOfRangeException("Wrong type as the argument to IGridDataSource<>");
                }

                _queryType = templateType.GetGenericArguments().Last();
                _sourceType = sourceType;

                return;
            }

            templateType = sourceType.FindInterfaceThatCloses(typeof (IPagedGridDataSource<,>));
            if (templateType != null)
            {
                if (templateType.GetGenericArguments().First() != typeof(T))
                {
                    throw new ArgumentOutOfRangeException("Wrong type as the argument to IGridDataSource<>");
                }

                _queryType = templateType.GetGenericArguments().Last();
                _sourceType = sourceType;

                return;
            }

            throw new ArgumentOutOfRangeException("TSource must be either IGridDataSource<T> or IGridDataSource<TQuery>");
        }

        public ColumnDefinition<T, TProp> Column<TProp>(Expression<Func<T, TProp>> property)
        {
            var column = new ColumnDefinition<T, TProp>(property, Projection);
            _columns.Add(column);

            return column;
        }

        public AccessorProjection<T, TProp> Data<TProp>(Expression<Func<T, TProp>> property)
        {
            return Projection.Value(property);
        }

        IEnumerable<IGridColumn> IGridDefinition.Columns()
        {
            return _columns;
        }

        Type IGridDefinition.DetermineRunnerType()
        {
            if (_sourceType == null) return null;

            if (_queryType != null && _queryType.CanBeCastTo<PagedQuery>())
            {
                return typeof(PagedGridRunner<,,,>).MakeGenericType(typeof(T), GetType(), _sourceType, _queryType);
            }

            return _queryType == null
                       ? typeof (GridRunner<,,>).MakeGenericType(typeof (T), GetType(), _sourceType)
                       : typeof (GridRunner<,,,>).MakeGenericType(typeof (T), GetType(), _sourceType, _queryType);
        }

       

        #region Nested type: AddExpression

        public class AddExpression
        {
            private readonly GridDefinition<T> _parent;

            public AddExpression(GridDefinition<T> parent)
            {
                _parent = parent;
            }

            public static AddExpression operator +(AddExpression original, IGridColumn column)
            {
                original._parent._columns.Add(column);

                return original;
            }
        }

        #endregion
    }
}