using System.Collections.Generic;
using System.Linq;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;

namespace MoNeriSharp.modules
{
    public class TransformerBlock : nn.Module
    {
        private MultiheadAttention attn;
        private LayerNorm norm1;
        private LayerNorm norm2;
        private Sequential feedForward;

        public TransformerBlock(string name, int embedDim, int numHeads, torch.Device device = null)
            : base(name)
        {
            if (device == null) device = torch.CPU;

            // 🔧 MultiheadAttention no acepta 'device' en el constructor
            attn = nn.MultiheadAttention(embedDim, numHeads);

            norm1 = nn.LayerNorm(embedDim, device: device);
            norm2 = nn.LayerNorm(embedDim, device: device);

            feedForward = nn.Sequential(
                ("fc1", nn.Linear(embedDim, embedDim * 4, device: device)),
                ("gelu", nn.GELU()),
                ("fc2", nn.Linear(embedDim * 4, embedDim, device: device))
            );

            RegisterComponents();
        }

        public torch.Tensor Forward(torch.Tensor x, torch.Tensor mask = null)
        {
            // 🔧 Pre‑LayerNorm + atención con residual
            var normed1 = norm1.forward(x);

            // 🔧 forward requiere key_padding_mask y need_weights
            var attnOut = attn.forward(normed1, normed1, normed1,
                                       key_padding_mask: null,
                                       need_weights: false,
                                       attn_mask: mask).Item1;

            x = x + attnOut;

            // 🔧 Pre‑LayerNorm + feed‑forward con residual
            var normed2 = norm2.forward(x);
            var ffOut = feedForward.forward(normed2);
            x = x + ffOut;

            return x;
        }

        public IEnumerable<Parameter> Parameters =>
            attn.parameters()
                .Concat(norm1.parameters())
                .Concat(norm2.parameters())
                .Concat(feedForward.parameters());
    }
}