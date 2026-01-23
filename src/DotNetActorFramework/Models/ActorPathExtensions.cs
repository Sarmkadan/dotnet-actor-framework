using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetActorFramework.Models
{
    /// <summary>
    /// Extension methods for <see cref="ActorPath"/>.
    /// </summary>
    public static class ActorPathExtensions
    {
        /// <summary>
        /// Returns the root (top‑most) <see cref="ActorPath"/> in the hierarchy.
        /// </summary>
        /// <param name="path">The path whose root is to be retrieved.</param>
        /// <returns>The root <see cref="ActorPath"/>; if <paramref name="path"/> has no parent, the same instance is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
        public static ActorPath GetRoot(this ActorPath path)
        {
            ArgumentNullException.ThrowIfNull(path);

            while (path.Parent is not null)
            {
                path = path.Parent;
            }

            return path;
        }

        /// <summary>
        /// Returns an ordered collection of all ancestors of the current <see cref="ActorPath"/>,
        /// starting with the immediate parent and ending with the root.
        /// </summary>
        /// <param name="path">The path whose ancestors are to be enumerated.</param>
        /// <returns>An <see cref="IReadOnlyList{ActorPath}"/> containing the ancestors.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
        public static IReadOnlyList<ActorPath> GetAncestors(this ActorPath path)
        {
            ArgumentNullException.ThrowIfNull(path);

            var ancestors = new List<ActorPath>();
            var current = path.Parent;
            while (current is not null)
            {
                ancestors.Add(current);
                current = current.Parent;
            }

            // The list is already ordered from immediate parent to root.
            return ancestors.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the current <see cref="ActorPath"/> is an ancestor of <paramref name="other"/>.
        /// </summary>
        /// <param name="path">The potential ancestor.</param>
        /// <param name="other">The path to test against.</param>
        /// <returns><c>true</c> if <paramref name="path"/> is an ancestor of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="other"/> is <c>null</c>.</exception>
        public static bool IsAncestorOf(this ActorPath path, ActorPath other)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(other);

            // Reuse the existing IsDescendantOf implementation on the other side.
            return other.IsDescendantOf(path);
        }

        /// <summary>
        /// Finds the deepest common ancestor of two <see cref="ActorPath"/> instances.
        /// </summary>
        /// <param name="first">The first path.</param>
        /// <param name="second">The second path.</param>
        /// <returns>
        /// The deepest common <see cref="ActorPath"/> shared by both arguments,
        /// or <c>null</c> if the paths share no common ancestor.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="first"/> or <paramref name="second"/> is <c>null</c>.</exception>
        public static ActorPath? GetCommonAncestor(this ActorPath first, ActorPath second)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);

            // Build the ancestor stacks for both paths.
            var firstAncestors = new Stack<ActorPath>();
            var secondAncestors = new Stack<ActorPath>();

            for (var p = first; p is not null; p = p.Parent) firstAncestors.Push(p);
            for (var p = second; p is not null; p = p.Parent) secondAncestors.Push(p);

            ActorPath? common = null;
            while (firstAncestors.Count > 0 && secondAncestors.Count > 0)
            {
                var a = firstAncestors.Pop();
                var b = secondAncestors.Pop();

                if (a.Equals(b))
                {
                    common = a;
                }
                else
                {
                    break;
                }
            }

            return common;
        }
    }
}
