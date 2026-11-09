using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Store.Application.Specification
{
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        protected BaseSpecification()
        {
        }
        
        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }
        public Expression<Func<T, bool>>? Criteria { get; private set; }
        public List<Expression<Func<T, object>>>? Includes { get; private set; } = new List<Expression<Func<T, object>>>();
        public Expression<Func<T, object>>? OrderBy { get; private set; }
        public Expression<Func<T, object>>? OrderByDescending { get; private set; }
        public int Take { get; private set; }
        public int Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }
        protected void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes!.Add(includeExpression);
        }
        protected void AddOrderBy(Expression<Func<T, object>> orderbyexpression)
        {
            OrderBy = orderbyexpression;
        }
        protected void AddOrderByDescending(Expression<Func<T, object>> orderbydescendingexpression)
        {
            OrderByDescending = orderbydescendingexpression;
        }
        protected void ApplyPaging(int skip,int take)
        {
            Take = take;
            Skip = skip;
            IsPagingEnabled = true;
        }
    }
}
