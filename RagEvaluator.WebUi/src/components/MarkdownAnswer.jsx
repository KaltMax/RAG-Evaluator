import { PropTypes } from "prop-types";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

/**
 * Renders an LLM-generated answer as Markdown (GitHub-flavored).
 * Styled via the Tailwind typography plugin tuned for the dark theme.
 * react-markdown does not render raw HTML, so the content is XSS-safe.
 */
function MarkdownAnswer({ children, className = "" }) {
  return (
    <div
      className={`prose prose-invert prose-sm max-w-none prose-p:text-gray-200 prose-headings:text-white prose-strong:text-white prose-a:text-blue-400 prose-code:text-blue-300 prose-pre:bg-[#161616] prose-pre:border prose-pre:border-gray-700 prose-li:text-gray-200 prose-table:text-gray-200 ${className}`}
    >
      <ReactMarkdown remarkPlugins={[remarkGfm]}>{children}</ReactMarkdown>
    </div>
  );
}

MarkdownAnswer.propTypes = {
  children: PropTypes.string,
  className: PropTypes.string,
};

export default MarkdownAnswer;
